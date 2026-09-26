using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

namespace Rivada.Updater;

internal static class Program
{
    [STAThread]
    static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--update")
        {
            try { var backup=Update(args[1]); File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"update-result.txt"),backup); return 0; }
            catch(Exception ex) { File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"update-result.txt"),ex.ToString()); return 1; }
        }
        ApplicationConfiguration.Initialize();
        using var form=new Form { Text="Rivada POS 1.6 — Güncelleme", Size=new Size(650,310),
            StartPosition=FormStartPosition.CenterScreen, FormBorderStyle=FormBorderStyle.FixedDialog, MaximizeBox=false,
            Font=new Font("Segoe UI",10), BackColor=Color.FromArgb(239,244,249) };
        var info=new Label { Text="Uygulamayı kapatın ve kurulu olduğu klasörü seçin.\nÖnce yedek alınır; işletme verileri korunur. Lisans gerekmez.",
            Location=new Point(20,20), Size=new Size(600,55) };
        var path=new TextBox { Text=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Rivada Restaurant POS"),
            Location=new Point(20,90), Size=new Size(490,28) };
        var browse=new Button { Text="Klasör…", Location=new Point(520,87), Size=new Size(95,34) };
        browse.Click+=(_,_)=> { using var dialog=new FolderBrowserDialog { SelectedPath=path.Text }; if(dialog.ShowDialog(form)==DialogResult.OK) path.Text=dialog.SelectedPath; };
        var apply=new Button { Text="Yedekle ve Güncelle", Location=new Point(20,140), Size=new Size(200,40) };
        var status=new Label { Text="ZIP paketini önce tamamen bir klasöre çıkarın.", Location=new Point(20,198), Size=new Size(600,60) };
        var busy=false;
        form.FormClosing+=(_,e)=>{ if(busy) e.Cancel=true; };
        apply.Click+=async (_,_)=> {
            var target=path.Text; busy=true; apply.Enabled=false; browse.Enabled=false; path.Enabled=false;
            status.Text="Dosyalar doğrulanıyor ve yedek alınıyor…";
            try {
                var backup=await Task.Run(()=>Update(target));
                status.Text="Güncelleme tamamlandı. Uygulamayı normal kısayolundan açabilirsiniz.";
                MessageBox.Show(form,"Güncelleme tamamlandı.\n\nYedek klasörü:\n"+backup,"Başarılı",MessageBoxButtons.OK,MessageBoxIcon.Information);
            } catch(Exception ex) { status.Text="Güncelleme tamamlanamadı."; MessageBox.Show(form,ex.Message,"Güncelleme",MessageBoxButtons.OK,MessageBoxIcon.Warning); }
            finally { busy=false; apply.Enabled=true; browse.Enabled=true; path.Enabled=true; }
        };
        form.Controls.AddRange(new Control[]{info,path,browse,apply,status});
        Application.Run(form);
        return 0;
    }

    private static string Update(string directory)
    {
        var target=Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar);
        var source=Path.GetFullPath(AppContext.BaseDirectory).TrimEnd(Path.DirectorySeparatorChar);
        if (Within(target,source) || Within(source,target)) throw new InvalidOperationException("Güncelleme paketini kurulu uygulama klasörünün dışında açın.");
        if (!File.Exists(Path.Combine(target,"Rivada.Restaurant.WinForms.exe"))) throw new InvalidOperationException("Seçilen klasörde kurulu Rivada uygulaması bulunamadı.");
        var processes=Process.GetProcessesByName("Rivada.Restaurant.WinForms");
        try { if(processes.Length>0) throw new InvalidOperationException("Önce açık Rivada Restaurant POS pencerelerini kapatın."); }
        finally { foreach(var process in processes) process.Dispose(); }
        var manifest=JsonSerializer.Deserialize<Dictionary<string,string>>(File.ReadAllText(Path.Combine(source,"package-manifest.json")))
            ?? throw new InvalidOperationException("Paket listesi okunamadı.");
        if (!manifest.ContainsKey("Rivada.Restaurant.WinForms.dll") || !manifest.ContainsKey("Rivada.Restaurant.WinForms.exe")) throw new InvalidOperationException("Paket eksik.");
        foreach(var entry in manifest)
        {
            var relative=entry.Key;
            var leaf=Path.GetFileName(relative);
            if(Path.IsPathRooted(relative) || relative.Split('/','\\').Any(x=>x=="..") || leaf.StartsWith("license",StringComparison.OrdinalIgnoreCase)
                || leaf.Contains(".db",StringComparison.OrdinalIgnoreCase) || relative.StartsWith("backups",StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("logs",StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Paket beklenmeyen veri dosyası içeriyor.");
            var file=Path.GetFullPath(Path.Combine(source,relative));
            if(!Within(file,source) || !Within(Path.GetFullPath(Path.Combine(target,relative)),target)) throw new InvalidOperationException("Geçersiz paket yolu.");
            using var stream=File.OpenRead(file);
            if(!Convert.ToHexString(SHA256.HashData(stream)).Equals(entry.Value,StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Paket dosyası eksik veya bozuk: "+relative);
        }
        var backup=Path.Combine(Path.GetDirectoryName(target)!,"Rivada POS Backups",DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(backup);
        foreach(var file in Directory.EnumerateFiles(target,"*",SearchOption.AllDirectories))
        {
            var relative=Path.GetRelativePath(target,file);
            if(relative.StartsWith("backups"+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase)) continue;
            var copy=Path.Combine(backup,relative);
            Directory.CreateDirectory(Path.GetDirectoryName(copy)!);
            File.Copy(file,copy);
        }
        var created=new List<string>();
        try
        {
            foreach(var entry in manifest)
            {
                var destination=Path.GetFullPath(Path.Combine(target,entry.Key));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
                if(!File.Exists(destination)) created.Add(destination);
                File.Copy(Path.Combine(source,entry.Key),destination,true);
            }
        }
        catch(Exception ex)
        {
            try {
                foreach(var entry in manifest)
                {
                    var old=Path.Combine(backup,entry.Key);
                    if(File.Exists(old)) File.Copy(old,Path.Combine(target,entry.Key),true);
                }
                foreach(var file in created) if(File.Exists(file)) File.Delete(file);
            } catch { throw new IOException("Güncelleme ve otomatik geri alma tamamlanamadı. Uygulamayı açmayın. Yedek: "+backup,ex); }
            throw new IOException("Güncelleme yapılamadı; önceki uygulama dosyaları geri yüklendi. Yedek: "+backup,ex);
        }
        return backup;
    }
    private static bool Within(string path,string root) => path.Equals(root,StringComparison.OrdinalIgnoreCase)
        || path.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase);
}
