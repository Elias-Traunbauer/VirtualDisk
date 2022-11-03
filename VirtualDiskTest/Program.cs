using System.Text;
using VirtualFilesystem;

namespace VirtualDiskTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DiskSettings settings = new DiskSettings(4000, 2000000000, 12, 24);
            using VirtualDisk disk = new VirtualDisk(@"C:\Users\trauni\Downloads\vdisk");

            if (!disk.DirectoryExists(@"V:\configs"))
                disk.CreateDirectory(@"V:\configs");
            disk.FileWriteAllBytes(@"V:\configs\project.zip", File.ReadAllBytes(@"C:\Users\trauni\Downloads\BakeryTemplate (1).zip"));
            VirtualFile file = disk.GetFile(@"V:\configs\project.zip");
            File.WriteAllBytes(@"C:\Users\trauni\Downloads\project.zip", file.ReadAllBytes());

            //disk = disk.Clone();

            //List<string> sussyGussy = new List<string>() { "sss", "sdsdsd" };
            //sussyGussy.Foreach(x => Console.WriteLine(x));
            //var ss = sussyGussy.Clone();
            //ss.Foreach(x => Console.WriteLine(x));

            //disk.CreateDirectory(@"V:\pics");
            //disk.FileWriteAllBytes(@"V:\configs\pic.png", File.ReadAllBytes(@"C:\Users\trauni\Downloads\project_20221021_1855366-01_2 - Copy.png"));
            //file = disk.GetFile(@"V:\configs\pic.png");
            //Console.WriteLine("File-size: " + file.Size);
            //File.WriteAllBytes(@"C:\Users\trauni\Downloads\pic.png", file.ReadAllBytes());
        }
    }
}