using System.Text;
using VirtualFilesystem;

namespace VirtualDiskTest
{
    internal class Program
    {
        static void Main(string[] args)
        {
            DiskSettings settings = new DiskSettings(4000, 2000000000, 12, 24);
            VirtualDisk disk = new VirtualDisk(settings);
            Console.WriteLine("New Disk:");
            Console.WriteLine("TotalSpace: " + DiskSettings.GetSizeRepresentation(disk.Settings.TotalSpace));
            Console.WriteLine("MaximumNameLength: " + DiskSettings.GetSizeRepresentation(disk.Settings.MaximumNameLength));
            Console.WriteLine("ActualSpacePerBlock: " + DiskSettings.GetSizeRepresentation(disk.Settings.ActualSpacePerBlock));
            Console.WriteLine("StorageSize: " + DiskSettings.GetSizeRepresentation(disk.Settings.StorageSize));
            Console.WriteLine("PointerSize: " + DiskSettings.GetSizeRepresentation(disk.Settings.PointerSize));
            Console.WriteLine("NodeEntrySize: " + DiskSettings.GetSizeRepresentation(disk.Settings.NodeEntrySize));
            Console.WriteLine("NodeTableSize: " + DiskSettings.GetSizeRepresentation(disk.Settings.NodeTableSize));
            Console.WriteLine("MaxItemsPerDirectory: " + disk.Settings.MaxItemsPerDirectory);
            Console.WriteLine("FreeSpace: " + DiskSettings.GetSizeRepresentation(disk.FreeSpace));

            Console.WriteLine();

            disk.CreateDirectory(@"V:\configs");
            disk.FileWriteAllBytes(@"V:\configs\sus.txt", File.ReadAllBytes(@"C:\Users\trauni\Downloads\sus.txt"));
            VirtualFile file = disk.GetFile(@"V:\configs\sus.txt");
            Console.WriteLine("File-size: " + file.Size);
            File.WriteAllBytes(@"C:\Users\trauni\Downloads\susy.txt", file.ReadAllBytes());

            disk = disk.Clone();

            List<string> sussyGussy = new List<string>() { "sss", "sdsdsd" };
            sussyGussy.Foreach(x => Console.WriteLine(x));

            disk.CreateDirectory(@"V:\pics");
            disk.FileWriteAllBytes(@"V:\configs\pic.png", File.ReadAllBytes(@"C:\Users\trauni\Downloads\project_20221021_1855366-01_2 - Copy.png"));
            file = disk.GetFile(@"V:\configs\pic.png");
            Console.WriteLine("File-size: " + file.Size);
            File.WriteAllBytes(@"C:\Users\trauni\Downloads\pic.png", file.ReadAllBytes());
        }
    }
}