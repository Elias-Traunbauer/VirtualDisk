using System;
using System.Runtime.Serialization;
using System.Text;

namespace VirtualFilesystem
{
    /// <summary>
    /// Provides a small virtual File System to store Files and Directories in. Can be saved as byte array or file.
    /// Root directory is V:\
    /// </summary>
    public class VirtualDisk : IDisposable
    {
        /// <summary>
        /// Base Structure:
        /// |Settings|NodeEntryTable|Blocks|
        /// NodeEntry Structure:
        /// |isFile|Name|FileInfo|Pointer|
        /// Block Structure:
        /// |PointerToNextFile|Data|
        /// </summary>
        private byte[] storage;
        private DiskSettings settings;
        private string savePath = "";
        private byte NodeTableStartIndex = DiskSettings.SettingsSaveSize;

        #region Contructors
        /// <summary>
        /// Initializes a existing virtual disk from a byte array
        /// </summary>
        /// <param name="storage">Bytes of a virtual disk</param>
        public VirtualDisk(byte[] storage)
        {
            this.storage = storage;
            LoadSettingsFromStorage();
        }

        /// <summary>
        /// Initializes a existing virtual disk from a file
        /// </summary>
        /// <param name="path">Path to the virtual disk file</param>
        public VirtualDisk(string path)
        {
            savePath = path;
            storage = System.IO.File.ReadAllBytes(savePath);
            LoadSettingsFromStorage();
        }

        /// <summary>
        /// Initializes a new virtual disk with default settings
        /// </summary>
        public VirtualDisk()
        {
            settings = DiskSettings.Default;
            InitializeStorage();
        }

        /// <summary>
        /// Initializes a new virtual disk with predefined settings
        /// </summary>
        /// <param name="settings"></param>
        public VirtualDisk(DiskSettings settings)
        {
            this.settings = settings;
            InitializeStorage();
        }
        #endregion

        #region Properties
        /// <summary>
        /// Returns the settings for this Virtual Disk. READ ONLY
        /// </summary>
        public DiskSettings Settings
        {
            get { return (DiskSettings)(byte[])settings; }
        }

        private long StorageStartIndex
        {
            get
            {
                return NodeTableStartIndex + settings.NodeTableSize;
            }
        }

        public long FreeSpace
        {
            get
            {
                long freeSpace = 0;
                for (long i = 0; i < settings.BlockCount; i += 1)
                {
                    byte[] nodeEntry = new byte[settings.NodeEntrySize];
                    Array.Copy(storage, NodeTableStartIndex + (i * settings.NodeEntrySize), nodeEntry, 0, settings.NodeEntrySize);
                    if (GetArrayCheckSum(nodeEntry) == 0)
                    {
                        freeSpace += settings.ActualSpacePerBlock;
                    }
                }
                return freeSpace;
            }
        }
        #endregion

        #region Public Methods
        public bool FileExists(string path)
        {
            var directoryId = GoToLastDirectory(path);
            var fileId = GetNodeAddressForItemInDirectory(directoryId, path.Split('\\')[^1]);
            return fileId >= 0;
        }

        public bool DirectoryExists(string path)
        {
            string[] pathParts = path.Split('\\');
            if (pathParts[0] != "V:")
            {
                throw new Exception("Invalid path");
            }
            long directoryId = -1;
            for (int i = 1; i < pathParts.Length - 1; i++)
            {
                directoryId = GetNodeAddressForItemInDirectory(directoryId, pathParts[i]);
                GetNodeEntry(directoryId, out bool isFile, out string _, out byte[] _, out long _);
                if (isFile)
                {
                    return false;
                }
            }
            long lastDirectoryId = GetNodeAddressForItemInDirectory(directoryId, pathParts[^1]);
            if (lastDirectoryId == -2)
            {
                return false;
            }
            else
            {
                GetNodeEntry(lastDirectoryId, out bool isFilee, out string _, out byte[] _, out long _);
                return !isFilee;
            }
        }

        public string[] DirectoryGetSubDirectories(string path)
        {
            if (path.EndsWith("\\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            long directoryId = GoToLastDirectory(path);
            if (directoryId != -2)
            {
                string[] directories = new string[settings.MaxItemsPerDirectory];
                int index = 0;
                long[] itemIds = GetDirectoryEntries(directoryId);
                foreach (var item in itemIds)
                {
                    GetNodeEntry(item, out bool isFile, out string name, out byte[] _, out long _);
                    if (!isFile)
                    {
                        directories[index] = path + "\\" + name;
                        index++;
                    }
                }
                string[] actualFiles = new string[index];
                for (int i = 0; i < index; i++)
                {
                    actualFiles[i] = directories[i];
                }
                return actualFiles;
            }
            else
            {
                throw new Exception("Invalid path");
            }
        }

        /// <summary>
        /// Gets files in a directory
        /// </summary>
        /// <param name="path">Path of the given directory</param>
        /// <returns>An array with the paths to the files in the directory</returns>
        public string[] DirectoryGetFiles(string path)
        {
            if (path.EndsWith("\\"))
            {
                path = path.Substring(0, path.Length - 1);
            }
            long directoryId = GoToLastDirectory(path);
            if (directoryId != -2)
            {
                string[] files = new string[settings.MaxItemsPerDirectory];
                int index = 0;
                long[] itemIds = GetDirectoryEntries(directoryId);
                foreach (var item in itemIds)
                {
                    GetNodeEntry(item, out bool isFile, out string name, out byte[] _, out long _);
                    if (isFile)
                    {
                        files[index] = path + "\\" + name;
                        index++;
                    }
                }
                string[] actualFiles = new string[index];
                for (int i = 0; i < index; i++)
                {
                    actualFiles[i] = files[i];
                }
                return actualFiles;
            }
            else
            {
                throw new Exception("Invalid path");
            }
        }

        /// <summary>
        /// Gets a file located at the given path
        /// </summary>
        /// <param name="path">Path to get file from</param>
        /// <returns>A object that contains info about the file found at the given path</returns>
        public VirtualFile GetFile(string path)
        {
            var directoryId = GoToLastDirectory(path);
            string name = path.Split('\\')[^1];
            long fileId;
            if (!FileExists(path))
            {
                throw new Exception("File not found");
            }
            else
            {
                fileId = GetNodeAddressForItemInDirectory(directoryId, name);
            }
            GetNodeEntry(fileId, out bool isFile, out string _, out byte[] fileInfo, out long pointer);
            if (!isFile)
            {
                throw new Exception("Path does not lead to file");
            }
            else
            {
                int size = BitConverter.ToInt32(fileInfo[0..4]);
                DateTime modifiedTime = new DateTime(BitConverter.ToInt64(fileInfo[4..12]));
                VirtualFile file = new VirtualFile();
                file.Name = name;
                file.Size = size;
                file.LastModified = modifiedTime;
                file.Path = path;
                file.VirtualDisk = this;
                return file;
            }
        }

        public VirtualDirectory GetDirectory(string path)
        {
            var directoryId = GoToLastDirectory(path);
            GetNodeEntry(directoryId, out bool isFile, out string _, out byte[] fileInfo, out long _);
            if (!isFile)
            {
                VirtualDirectory dir = new VirtualDirectory();
                dir.Path = path;
                dir.VirtualDisk = this;
                DateTime modifiedTime = new DateTime(BitConverter.ToInt64(fileInfo[0..8]));
                dir.LastModified = modifiedTime;
                return dir;
            }
            else
            {
                throw new Exception("Path did not lead to directory.");
            }
        }

        public bool CreateDirectory(string path)
        {
            var directoryId = GoToLastDirectory(path);
            string name = path.Split('\\')[^1];
            long dirId;
            if (!DirectoryExists(path))
            {
                var res = RegisterNewItem(directoryId, name, false, new byte[0], out dirId);
                return res;
            }
            return false;
        }

        public byte[] FileReadAllBytes(string path)
        {
            var directoryId = GoToLastDirectory(path);
            string name = path.Split('\\')[^1];
            long fileId;
            if (!FileExists(path))
            {
                throw new Exception("File not found");
            }
            else
            {
                fileId = GetNodeAddressForItemInDirectory(directoryId, name);
            }
            GetNodeEntry(fileId, out bool isFile, out string _, out byte[] fileInfo, out long pointer);
            if (!isFile)
            {
                throw new Exception("Path does not lead to file");
            }
            else
            {
                int fileSize = BitConverter.ToInt32(fileInfo, 0);
                byte[] data = new byte[fileSize];
                int blockCount = (int)Math.Round((double)data.Length / (double)settings.ActualSpacePerBlock + 0.5d, 0);
                long nextPointer = pointer;
                int pastePos = 0;
                for (int i = 0; i < blockCount; i++)
                {
                    byte[] block = ReadBlock(nextPointer);
                    byte[] newPointer = block[0..settings.PointerSize];
                    int copyAmount = Math.Min(fileSize, settings.ActualSpacePerBlock);
                    fileSize -= settings.ActualSpacePerBlock;
                    Array.Copy(block, settings.PointerSize, data, pastePos, copyAmount);
                    pastePos += copyAmount;
                    if (settings.PointerSize == 1)
                    {
                        nextPointer = newPointer[0];
                    }
                    else if (settings.PointerSize == 2)
                    {
                        nextPointer = BitConverter.ToInt16(newPointer);
                    }
                    else if (settings.PointerSize == 4)
                    {
                        nextPointer = BitConverter.ToInt32(newPointer);
                    }
                    else
                    {
                        nextPointer = BitConverter.ToInt64(newPointer);
                    }
                }
                return data;
            }
        }

        public void FileWriteAllBytes(string path, byte[] data)
        {
            var directoryId = GoToLastDirectory(path);
            string name = path.Split('\\')[^1];
            long fileId;
            byte[] fileInfo = new byte[settings.FileInfoSize];
            byte[] sizeAsByteArray = BitConverter.GetBytes(data.Length);
            Array.Copy(sizeAsByteArray, 0, fileInfo, 0, sizeAsByteArray.Length);
            if (FileExists(path))
            {
                FileDelete(path);
            }
            RegisterNewItem(directoryId, name, true, fileInfo, out fileId);
            GetNodeEntry(fileId, out bool isFile, out string _, out byte[] _, out long pointer);
            if (!isFile)
            {
                throw new Exception("Path does not lead to file");
            }
            else
            {
                int blockCount = (int)Math.Round((double)data.Length / (double)settings.ActualSpacePerBlock + 0.5d, 0);
                long previousPointer = pointer;
                if (blockCount > 1)
                {
                    for (int i = 0; i < blockCount; i++)
                    {
                        int dataStartIndex = i * settings.ActualSpacePerBlock;
                        int dataEndIndex = Math.Min(dataStartIndex + settings.ActualSpacePerBlock, data.Length - 1);
                        GetFreeBlock(out byte[] newPointer, previousPointer);
                        if (i == blockCount - 1)
                        {
                            Array.Clear(newPointer, 0, newPointer.Length);
                        }
                        byte[] block = new byte[settings.BlockSize];
                        Array.Copy(data, dataStartIndex, block, 4, dataEndIndex - dataStartIndex);
                        Array.Copy(newPointer, 0, block, 0, settings.PointerSize);
                        WriteBlock(previousPointer, block);
                        if (settings.PointerSize == 1)
                        {
                            previousPointer = newPointer[0];
                        }
                        else if (settings.PointerSize == 2)
                        {
                            previousPointer = BitConverter.ToInt16(newPointer);
                        }
                        else if (settings.PointerSize == 4)
                        {
                            previousPointer = BitConverter.ToInt32(newPointer);
                        }
                        else
                        {
                            previousPointer = BitConverter.ToInt64(newPointer);
                        }
                    }
                }
                else
                {
                    byte[] block = new byte[settings.BlockSize];
                    Array.Copy(data, 0, block, 4, data.Length);
                    WriteBlock(pointer, block);
                }
            }
        }

        public void FileDelete(string path)
        {
            var directoryId = GoToLastDirectory(path);
            string name = path.Split('\\')[^1];
            if (FileExists(path))
            {
                long fileId = GetNodeAddressForItemInDirectory(directoryId, name);
                GetNodeEntry(fileId, out bool _, out string _, out byte[] _, out long pointer);
                FreeBlocks(pointer);
                FreeNode(fileId);
            }
            else
            {
                throw new Exception("File not found");
            }
        }

        public void GetFreeBlock(out byte[] pointer, params long[] exclude)
        {
            pointer = new byte[settings.PointerSize];

            byte[] block = new byte[settings.BlockSize];
            long freeBlockPointer = 0;
            for (long i = 1; i < settings.BlockCount; i++)
            {
                block = ReadBlock(StorageStartIndex + i * settings.BlockSize);
                if (GetArrayCheckSum(block) == 0)
                {
                    bool excludeF = false;
                    for (int j = 0; j < exclude.Length; j++)
                    {
                        if (StorageStartIndex + i * settings.BlockSize == exclude[j])
                        {
                            excludeF = true;
                        }
                    }
                    if (!excludeF)
                    {
                        freeBlockPointer = i;
                        break;
                    }
                }
            }

            freeBlockPointer = StorageStartIndex + freeBlockPointer * settings.BlockSize;

            if (settings.PointerSize == 1)
            {
                pointer = new byte[] { (byte)freeBlockPointer };
            }
            else if (settings.PointerSize == 2)
            {
                pointer = BitConverter.GetBytes((short)freeBlockPointer);
            }
            else if(settings.PointerSize == 4)
            {
                pointer = BitConverter.GetBytes((int)freeBlockPointer);
            }
            else
            {
                pointer = BitConverter.GetBytes(freeBlockPointer);
            }
        }

        public void SaveVirtualDiskToFile(string path)
        {
            System.IO.File.WriteAllBytes(path, storage);
        }

        public byte[] SaveVirtualDiskToByteArray()
        {
            return storage;
        }

        public void Dispose()
        {
            if (savePath != "")
            {
                SaveVirtualDiskToFile(savePath);
            }
            GC.Collect();
        }
        #endregion

        #region Private Methods
        private void FreeBlocks(long pointer)
        {
            long nextBlockPointer = pointer;
            do
            {
                byte[] block = ReadBlock(nextBlockPointer);
                EvaluateBlock(block, out nextBlockPointer, out byte[] _);
            } while (nextBlockPointer != 0);
        }

        private ulong GetArrayCheckSum(byte[] data)
        {
            ulong sum = 0;
            for (long i = 0; i < data.Length; i++)
            {
                sum += data[i];
            }
            return sum;
        }

        private ulong GetArrayCheckSum(byte[] data, long start, long length)
        {
            ulong sum = 0;
            for (long i = start; i < start + length; i++)
            {
                sum += data[i];
            }
            return sum;
        }

        private long GoToLastDirectory(string path)
        {
            string[] pathParts = path.Split('\\');
            if (pathParts[0] != "V:")
            {
                return -2;
            }
            long directoryId = -1;
            for (int i = 1; i < pathParts.Length - 1; i++)
            {
                directoryId = GetNodeAddressForItemInDirectory(directoryId, pathParts[i]);
                GetNodeEntry(directoryId, out bool isFile, out string _, out byte[] _, out long _);
                if (isFile)
                {
                    return -2;
                }
            }
            long lastDirectoryId = GetNodeAddressForItemInDirectory(directoryId, pathParts[^1]);
            if (lastDirectoryId == -2)
            {
                return directoryId;
            }
            else
            {
                GetNodeEntry(lastDirectoryId, out bool isFilee, out string _, out byte[] _, out long _);
                if (!isFilee)
                {
                    directoryId = lastDirectoryId;
                }
                return directoryId;
            }
        }

        private void LoadSettingsFromStorage()
        {
            settings = (DiskSettings)storage[0..12];
        }

        private void InitializeStorage()
        {
            storage = new byte[settings.StorageSize];
            Array.Copy((byte[])settings, storage, 12);
        }

        private byte[] ReadBlock(long pointer)
        {
            long blockId = StorageStartIndex + pointer;
            byte[] data = new byte[settings.BlockSize];
            Array.Copy(storage, blockId, data, 0, settings.BlockSize);
            return data;
        }

        private void WriteBlock(long pointer, byte[] data)
        {
            long blockId = StorageStartIndex + pointer;
            Array.Copy(data, 0, storage, blockId, data.Length);
        }

        private void EvaluateBlock(byte[] data, out long nextBlock, out byte[] fileData)
        {
            nextBlock = BitConverter.ToInt64(data[0..settings.PointerSize]);
            fileData = data[settings.BlockDataIndex..];
        }

        private long[] GetDirectoryEntries(long nodeId)
        {
            GetNodeEntry(nodeId, out bool isFile, out string name, out byte[] _, out long pointer);
            long[] entries = new long[settings.MaxItemsPerDirectory];
            int index = 0;
            if (!isFile)
            {
                byte[] directoryData = ReadBlock(pointer);
                for (long i = 0; i < settings.MaxItemsPerDirectory; i++)
                {
                    byte[] directoryEntry = new byte[DiskSettings.NodeEntryPointerSize];
                    Array.Copy(directoryData, i * DiskSettings.NodeEntryPointerSize, directoryEntry, 0, directoryEntry.Length);
                    if (GetArrayCheckSum(directoryEntry) != 0)
                    {
                        entries[index] = BitConverter.ToInt64(directoryEntry);
                        index++;
                    }
                }
                long[] actualEntries = new long[index];
                for (int i = 0; i < index; i++)
                {
                    actualEntries[i] = entries[i];
                }
                return actualEntries;
            }
            else
            {
                throw new Exception("Node-id led to a file");
            }
        }

        private void FreeNode(long nodeId)
        {
            int nodePointer = (int)(NodeTableStartIndex + nodeId * settings.NodeEntrySize);
            Array.Clear(storage, nodePointer, settings.NodeEntrySize);
        }

        private long GetNodeAddressForItemInDirectory(long directoryId, string itemName)
        {
            long[] entries = GetDirectoryEntries(directoryId);
            foreach (var entry in entries)
            {
                GetNodeEntry(entry, out bool isFile, out string name, out byte[] _, out long _);
                if (name == itemName)
                {
                    return entry;
                }
            }
            return -2;
        }

        private void GetNodeEntry(long nodeId, out bool isFile, out string name, out byte[] fileInfo, out long pointer)
        {
            if (nodeId == -1)
            {
                // root directory
                isFile = false;
                name = "V:";
                fileInfo = new byte[0];
                pointer = StorageStartIndex;
            }
            else
            {
                // get node entry bytes
                long nodeEntryPointer = NodeTableStartIndex + nodeId * settings.NodeEntrySize;
                byte[] nodeEntry = new byte[settings.NodeEntrySize];
                if (nodeEntryPointer < 0)
                {
                    throw new DirectoryNotFoundException();
                }
                Array.Copy(storage, nodeEntryPointer, nodeEntry, 0, nodeEntry.Length);

                if (GetArrayCheckSum(nodeEntry) == 0)
                {
                    pointer = -1;
                    isFile = true;
                    name = null;
                    fileInfo = null;
                    return;
                }

                int currentDataIndex = 0;

                // Determine type
                isFile = nodeEntry[currentDataIndex] == 0;
                currentDataIndex++;

                // Determine name
                byte[] nameAsByteArray = new byte[settings.MaximumNameLength];
                Array.Copy(nodeEntry, currentDataIndex, nameAsByteArray, 0, nameAsByteArray.Length);
                currentDataIndex += nameAsByteArray.Length;
                int nameLength = 0;
                for (int i = 0; i < nameAsByteArray.Length; i++)
                {
                    if (nameAsByteArray[i] != 0)
                    {
                        nameLength++;
                    }
                    else
                    {
                        break;
                    }
                }
                name = Encoding.ASCII.GetString(nameAsByteArray[0..nameLength]);

                // Determine fileInfo
                fileInfo = new byte[settings.FileInfoSize];
                Array.Copy(nodeEntry, currentDataIndex, fileInfo, 0, fileInfo.Length);
                currentDataIndex += settings.FileInfoSize;

                // Determine pointer
                byte[] pointerAsByteArray = new byte[settings.PointerSize];
                Array.Copy(nodeEntry, currentDataIndex, pointerAsByteArray, 0, pointerAsByteArray.Length);
                if (settings.PointerSize == 1)
                {
                    pointer = pointerAsByteArray[0];
                }
                else if (settings.PointerSize == 2)
                {
                    pointer = BitConverter.ToInt16(pointerAsByteArray);
                }
                else if (settings.PointerSize == 4)
                {
                    pointer = BitConverter.ToInt32(pointerAsByteArray);
                }
                else
                {
                    pointer = BitConverter.ToInt64(pointerAsByteArray);
                }
            }
        }

        private bool RegisterNewItem(long parentDirectoryNodeId, string name, bool isFile, byte[] fileInfo, out long nodeId)
        {
            nodeId = -1;

            // Get dir info
            GetNodeEntry(parentDirectoryNodeId, out bool dirIsFile, out _, out _, out long dirPointer);
            if (!dirIsFile)
            {
                byte[] parentDirectoryData = ReadBlock(dirPointer);
                int freeDirectoryIndex = -1;
                long freeNodeEntryId = -1;
                // Get free directory entry
                byte[] directoryEntryAsBytes = new byte[DiskSettings.NodeEntryPointerSize];
                for (int i = 0; i < settings.MaxItemsPerDirectory; i++)
                {
                    Array.Copy(parentDirectoryData, i * DiskSettings.NodeEntryPointerSize, directoryEntryAsBytes, 0, directoryEntryAsBytes.Length);
                    if (GetArrayCheckSum(directoryEntryAsBytes) == 0)
                    {
                        freeDirectoryIndex = i;
                        break;
                    }
                }

                // Get free node entry id
                byte[] nodeEntryTable = new byte[settings.NodeTableSize];
                Array.Copy(storage, NodeTableStartIndex, nodeEntryTable, 0, nodeEntryTable.Length);
                for (int i = 1; i < nodeEntryTable.Length / settings.NodeEntrySize; i++)
                {
                    GetNodeEntry(i, out _, out _, out _, out long pointer);
                    if (pointer == -1)
                    {
                        freeNodeEntryId = i;
                        break;
                    }
                }

                // Get free block
                byte[] block = new byte[settings.BlockSize];
                long freeBlockPointer = 0;
                for (long i = 1; i < settings.BlockCount; i++)
                {
                    block = ReadBlock(StorageStartIndex + i * settings.BlockSize);
                    if (GetArrayCheckSum(block) == 0 && StorageStartIndex + i * settings.BlockSize != dirPointer)
                    {
                        freeBlockPointer = i;
                        break;
                    }
                }
                freeBlockPointer = StorageStartIndex + freeBlockPointer * settings.BlockSize;
                if (freeDirectoryIndex != -1 && freeNodeEntryId != -1)
                {
                    // everything found and write will be executed
                    byte[] newNodeEntry = new byte[settings.NodeEntrySize];
                    newNodeEntry[0] = (byte)(isFile ? 0 : 255);
                    byte[] nameAsByteArray = new byte[settings.MaximumNameLength];
                    var nameBytes = Encoding.ASCII.GetBytes(name);
                    Array.Copy(nameBytes, 0, nameAsByteArray, 0, nameBytes.Length);
                    Array.Copy(nameAsByteArray, 0, newNodeEntry, 1, nameAsByteArray.Length);
                    Array.Copy(fileInfo, 0, newNodeEntry, 1 + settings.MaximumNameLength, fileInfo.Length);
                    byte[] pointerBytes;
                    if (settings.PointerSize == 1)
                    {
                        pointerBytes = new byte[] { (byte)freeBlockPointer };
                    }
                    else if (settings.PointerSize == 2)
                    {
                        pointerBytes = BitConverter.GetBytes((short)freeBlockPointer);
                    }
                    else if (settings.PointerSize == 4)
                    {
                        pointerBytes = BitConverter.GetBytes((int)freeBlockPointer);
                    }
                    else
                    {
                        pointerBytes = BitConverter.GetBytes(freeBlockPointer);
                    }
                    Array.Copy(pointerBytes, 0, newNodeEntry, 1 + settings.MaximumNameLength + settings.FileInfoSize, pointerBytes.Length);
                    // save node entry
                    Array.Copy(newNodeEntry, 0, storage, NodeTableStartIndex + freeNodeEntryId * settings.NodeEntrySize, newNodeEntry.Length);
                    // save directory entry
                    byte[] directoryNodePointer = BitConverter.GetBytes(freeNodeEntryId);
                    Array.Copy(directoryNodePointer, 0, parentDirectoryData, freeDirectoryIndex * DiskSettings.NodeEntryPointerSize, directoryNodePointer.Length);
                    WriteBlock(dirPointer, parentDirectoryData);
                    nodeId = freeNodeEntryId;
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        #endregion
    }

    [Serializable]
    internal class DirectoryNotFoundException : Exception
    {
        public DirectoryNotFoundException()
        {
        }

        public DirectoryNotFoundException(string message) : base(message)
        {
        }

        public DirectoryNotFoundException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DirectoryNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    #region VirtualItems
    /// <summary>
    /// Basic representation of an item in a Virtual Disk
    /// </summary>
    public interface IVirtualItem
    {
        public VirtualDisk VirtualDisk { get; }
        public string Path { get; }
        public string Name { get; }
        public DateTime LastModified { get; }
    }

    /// <summary>
    /// Representation of a file in a Virtual Disk
    /// </summary>
    public class VirtualFile : IVirtualItem
    {
        public string Path { get; internal set; }
        public DateTime LastModified { get; internal set; }
        public long Size { get; internal set; }
        public string Name { get; internal set; }
        public VirtualDisk VirtualDisk { get; internal set; }

        internal VirtualFile()
        {

        }

        /// <summary>
        /// Reads all bytes from the given file
        /// </summary>
        /// <returns>An array with the file content</returns>
        public byte[] ReadAllBytes()
        {
            return VirtualDisk.FileReadAllBytes(Path);
        }

        /// <summary>
        /// Writes bytes into the file. If file had contents previously, content is overridden
        /// </summary>
        /// <param name="data"></param>
        public void WriteAllBytes(byte[] data)
        {
            VirtualDisk.FileWriteAllBytes(Path, data);
        }

        public void Delete()
        {
            VirtualDisk.FileDelete(Path);
        }
    }

    /// <summary>
    /// Representation of a directory in a Virtual Disk
    /// </summary>
    public class VirtualDirectory : IVirtualItem
    {
        public string Path { get; internal set; }
        public string Name { get; internal set; }
        public DateTime LastModified { get; internal set; }
        public VirtualDisk VirtualDisk { get; internal set; }

        internal VirtualDirectory()
        {

        }

        public string[] GetSubDirectories(string path)
        {
            return VirtualDisk.DirectoryGetSubDirectories(path);
        }

        public string[] GetFiles(string path)
        {
            return VirtualDisk.DirectoryGetFiles(path);
        }
    }
    #endregion

    #region DiskSettings
    /// <summary>
    /// Provides all necessary settings for a Virtual Disk
    /// </summary>
    public class DiskSettings
    {
        /// <summary>
        /// An array to represent common file sizes.
        /// </summary>
        public static string[] fileSizes = new string[]
        {
            "B",
            "KB",
            "MB",
            "GB",
            "TB"
        };

        public static byte SettingsSaveSize = 12;
        private ushort blockSize;
        private byte fileInfoSize;
        private long storageSize;
        private byte maxNameLength;

        public const byte NodeEntryPointerSize = 8;

        /// <summary>
        /// Size value given in bytes
        /// </summary>
        public ushort BlockSize
        {
            get { return blockSize; }
        }

        /// <summary>
        /// Size value given in bytes
        /// </summary>
        public byte FileInfoSize
        {
            get { return fileInfoSize; }
        }

        /// <summary>
        /// Total storage size. Size value given in bytes
        /// </summary>
        public long StorageSize
        {
            get { return storageSize; }
        }

        /// <summary>
        /// Maximum length for a file/directory name
        /// </summary>
        public byte MaximumNameLength
        {
            get { return maxNameLength; }
        }

        /// <summary>
        /// Calculates the index inside a block where the data starts. Size value given in bytes
        /// </summary>
        public ushort BlockDataIndex
        {
            get
            {
                return (byte)(fileInfoSize + PointerSize + 1);
            }
        }

        /// <summary>
        /// Calculates how much space is used for a pointer to a block. Size value given in bytes
        /// </summary>
        public byte PointerSize
        {
            get
            {
                long blockCount = storageSize / blockSize;
                byte pointerSize = 1;
                while (Math.Pow(255ul, pointerSize) < blockCount)
                {
                    pointerSize *= 2;
                }
                return pointerSize;
            }
        }

        /// <summary>
        /// Calculates how much blocks are available. Size value given in bytes
        /// </summary>
        public long BlockCount
        {
            get
            {
                return (StorageSize - NodeTableSize - 12) / blockSize;
            }
        }

        /// <summary>
        /// Calculates how much space the node table will consume at max. Size value given in bytes
        /// </summary>
        public uint NodeTableSize
        {
            get
            {
                uint nodeTableSize = (uint)(NodeEntrySize * (long)(storageSize * 0.9 / blockSize));
                return nodeTableSize;
            }
        }

        /// <summary>
        /// Calculates the size needed for a node entry. Size value given in bytes
        /// </summary>
        public ushort NodeEntrySize
        {
            get
            {
                ushort nodeEntrySize = (ushort)(maxNameLength + PointerSize + fileInfoSize + 1);
                return nodeEntrySize;
            }
        }

        /// <summary>
        /// Maximum files that can be stored in a directory
        /// </summary>
        public int MaxItemsPerDirectory
        {
            get
            {
                int fileCount = (blockSize - PointerSize) / NodeEntryPointerSize;
                return fileCount;
            }
        }

        /// <summary>
        /// Total space usable for files. Size value given in bytes
        /// </summary>
        public long TotalSpace
        {
            get
            {
                long totalSize = storageSize - 12;
                totalSize -= NodeTableSize;
                return totalSize;
            }
        }

        /// <summary>
        /// Size per block for actual file data Size value given in bytes
        /// </summary>
        public ushort ActualSpacePerBlock
        {
            get
            {
                ushort totalSize = blockSize;
                totalSize -= PointerSize;
                return totalSize;
            }
        }

        /// <summary>
        /// Initializes a new Disk Settings instance from given parameters
        /// </summary>
        /// <param name="blockSize"></param>
        /// <param name="storageSize"></param>
        /// <param name="fileInfoSize"></param>
        /// <param name="maxNameLength"></param>
        public DiskSettings(ushort blockSize, long storageSize, byte fileInfoSize, byte maxNameLength)
        {
            this.blockSize = blockSize;
            this.fileInfoSize = fileInfoSize;
            this.storageSize = storageSize;
            this.maxNameLength = maxNameLength;
        }

        private DiskSettings()
        {

        }

        public static explicit operator DiskSettings(byte[] data)
        {
            DiskSettings settings = new DiskSettings();
            settings.blockSize = BitConverter.ToUInt16(data, 0);
            settings.fileInfoSize = data[2];
            settings.storageSize = BitConverter.ToInt64(data, 3);
            settings.maxNameLength = data[11];
            return settings;
        }

        public static explicit operator byte[](DiskSettings settings)
        {
            byte[] data = new byte[SettingsSaveSize];
            ushort blockSize = settings.BlockSize;
            byte fileInfoSize = settings.FileInfoSize;
            long storageSize = settings.storageSize;
            byte maxNameLength = settings.MaximumNameLength;
            unsafe
            {
                byte* bsBytes = (byte*)&blockSize;
                byte* ssBytes = (byte*)&storageSize;
                data[0] = bsBytes[0];
                data[1] = bsBytes[1];
                data[2] = fileInfoSize;
                data[3] = ssBytes[0];
                data[4] = ssBytes[1];
                data[5] = ssBytes[2];
                data[6] = ssBytes[3];
                data[7] = ssBytes[4];
                data[8] = ssBytes[5];
                data[9] = ssBytes[6];
                data[10] = ssBytes[7];
                data[11] = maxNameLength;
            }
            return data;
        }

        public static DiskSettings Default
        {
            get
            {
                DiskSettings settings = new DiskSettings(1024 * 4, 1024 * 1024 * 500, 128, 128);
                return settings;
            }
        }

        /// <summary>
        /// Calculates a well readable representation from a given size in bytes
        /// </summary>
        /// <param name="size">Size to convert in bytes</param>
        /// <param name="digits">Count of digits after the comma</param>
        /// <returns>Well readable presentation of the given size</returns>
        public static string GetSizeRepresentation(long size, byte digits = 2)
        {
            double sizeDouble = size;
            byte fileSizeIndex = 0;
            while (sizeDouble > 1024)
            {
                fileSizeIndex++;
                sizeDouble /= 1024;
            }
            return $"{Math.Round(sizeDouble, digits)}{fileSizes[fileSizeIndex]}";
        }
    }
    #endregion
}
