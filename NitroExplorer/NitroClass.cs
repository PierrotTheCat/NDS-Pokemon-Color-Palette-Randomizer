using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace NitroExplorer {
    public class NitroClass {
        /* Base */
        public string ROMFilename;
        public uint NTOffset;
        public uint NTSize;
        public uint FATOffset;
        public uint FATSize;
        /* Files */
        public Dictionary<string,ushort> FileIDs;
        public Dictionary<ushort,string> FileNames;
        public Dictionary<ushort,uint> FileOffsets;
        public Dictionary<ushort,uint> FileSizes;
        /* Misc */
        public ushort FileCount;
        public uint FileLastEnd;

        private FileStream rfs;

        private Stopwatch TimeThis;

        public delegate void ROMIsReadyD(long TimeTakenMS);
        public delegate void DirReadyD(int DirID, int ParentID, string DirName, bool IsRoot);
        public delegate void FileReadyD(int FileID, int ParentID, string FileName);

        public event ROMIsReadyD ROMIsReady;
        public event DirReadyD DirReady;
        public event FileReadyD FileReady;

        /* Load a ROM */
        public void LoadROM(string FileName) {
            TimeThis = new Stopwatch();
            TimeThis.Start();

            // init stream
            rfs = new FileStream(FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            rfs.Seek(0x40, SeekOrigin.Begin);

            // obtain base info
            NTOffset = ReadUInt(rfs);
            NTSize = ReadUInt(rfs);
            FATOffset = ReadUInt(rfs);
            FATSize = ReadUInt(rfs);
            ROMFilename = FileName;
            
            // reset dictionaries
            if (FileIDs == null) {
                // not initialised!
                FileIDs = new Dictionary<string, ushort>();
                FileNames = new Dictionary<ushort, string>();
                FileOffsets = new Dictionary<ushort,uint>();
                FileSizes = new Dictionary<ushort,uint>();
            }
            FileIDs.Clear();
            FileNames.Clear();
            FileOffsets.Clear();
            FileSizes.Clear();

            // start reading
            LoadDir("Root", 61440, 0);

            // clear up
            rfs.Dispose();
            TimeThis.Stop();
            if (ROMIsReady != null) ROMIsReady(TimeThis.ElapsedMilliseconds);
        }

        /* Load a Directory */
        private void LoadDir(string DirName, ushort DirID, ushort Parent) {
            long PreviousSeek = rfs.Position;
            rfs.Seek(NTOffset + (8 * (DirID & 0xFFF)), SeekOrigin.Begin);
            uint EntryStart = ReadUInt(rfs);
            ushort EntryFileID = ReadUShort(rfs);
            ushort ParentID = ReadUShort(rfs);

            // list to class owner
            if (Parent == 0) {
                if (DirReady != null) DirReady(61440, 0, "Root", true);
            } else {
                if (DirReady != null) DirReady(DirID, Parent, DirName, false);
            }

            // read FNT entries
            rfs.Seek(NTOffset + EntryStart, SeekOrigin.Begin);
            ushort CurFile = EntryFileID;
            while (true) {
                byte EntryLength = (byte)rfs.ReadByte();
                byte NameLength = (byte)(EntryLength & 127);
                if (NameLength == 0) break;
                // read entry
                string EntryName = ReadString(rfs, NameLength);
                if (EntryLength > 127) {
                    // directory
                    ushort SubTableID = ReadUShort(rfs);
                    LoadDir(EntryName, SubTableID, DirID);
                } else {
                    // file
                    LoadFile(EntryName, CurFile, DirID);
                }
                CurFile++;
            }

            rfs.Seek(PreviousSeek, SeekOrigin.Begin);
        }

        /* Load a File */
        private void LoadFile(string FileName, ushort FileID, ushort Parent) {
            long PreviousSeek = rfs.Position;
            rfs.Seek(FATOffset + (FileID * 8), SeekOrigin.Begin);
            uint FATStart = ReadUInt(rfs);
            uint FATEnd = ReadUInt(rfs);
            if (FileReady != null) FileReady(FileID, Parent, FileName);
            FileIDs[FileName] = FileID;
            FileNames[FileID] = FileName;
            FileOffsets[FileID] = FATStart;
            FileSizes[FileID] = FATEnd - FATStart;
            if (FATEnd > FileLastEnd) FileLastEnd = FATEnd;
            FileCount++;
            rfs.Seek(PreviousSeek, SeekOrigin.Begin);
        }

        /* Extract a File */
        public byte[] ExtractFile(ushort FileID) {
            rfs = new FileStream(ROMFilename, FileMode.Open, FileAccess.Read, FileShare.Read);
            rfs.Seek(FileOffsets[FileID], SeekOrigin.Begin);
            byte[] TempFile = new byte[FileSizes[FileID]];
            rfs.Read(TempFile, 0, (int)FileSizes[FileID]);
            rfs.Dispose();
            return TempFile;
        }

        /* Reinsert a File */
        public void ReplaceFile(ushort FileID, byte[] NewFile) {
            rfs = new FileStream(ROMFilename, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            uint OldOffset = FileOffsets[FileID];
            uint TempEnd;
            int cDiff = (int)(NewFile.Length - FileSizes[FileID]);
            if (cDiff > 0) {
                uint Diff = (uint)cDiff;
                Dictionary<ushort, string>.KeyCollection EnumThis = FileNames.Keys;
                foreach (ushort ModID in EnumThis) {
                    uint ModOffset = FileOffsets[ModID];
                    if (ModOffset > OldOffset) {
                        ModOffset += Diff;
                        FileOffsets[ModID] = ModOffset;
                        rfs.Seek(FATOffset + (ModID * 8), SeekOrigin.Begin);
                        WriteUInt(rfs, ModOffset);
                        TempEnd = ReadUInt(rfs) + Diff;
                        rfs.Seek(-4, SeekOrigin.Current);
                        WriteUInt(rfs, TempEnd);
                    }
                }
                rfs.Seek(OldOffset, SeekOrigin.Begin);
                byte[] MoveBuffer = new byte[FileLastEnd - OldOffset + 1];
                rfs.Read(MoveBuffer, 0, MoveBuffer.Length);
                rfs.Seek(OldOffset + Diff, SeekOrigin.Begin);
                rfs.Write(MoveBuffer, 0, MoveBuffer.Length);
                FileLastEnd += Diff;
            }
            rfs.Seek(OldOffset, SeekOrigin.Begin);
            rfs.Write(NewFile, 0, NewFile.Length);
            rfs.Seek(FATOffset + 4 + (FileID * 8), SeekOrigin.Begin);
            TempEnd = ReadUInt(rfs);
            rfs.Seek(-4, SeekOrigin.Current);
            WriteUInt(rfs, Convert.ToUInt32((int)TempEnd + cDiff));
            FileSizes[FileID] = (uint)NewFile.Length;
            rfs.Dispose();
        }

        /* Supporting Functions */
        private uint ReadUInt(FileStream fs) {
            // get an unsigned int from a passed filestream
            // operates in little-endian
            byte[] TempByte = new byte[4];
            fs.Read(TempByte, 0, 4);
            uint NewVal = TempByte[0];
            NewVal += (uint)TempByte[1] * 0x100;
            NewVal += (uint)TempByte[2] * 0x10000;
            NewVal += (uint)TempByte[3] * 0x1000000;
            return NewVal;
        }

        private ushort ReadUShort(FileStream fs) {
            // get an unsigned short from a passed filestream
            // operates in little-endian
            byte[] TempByte = new byte[2];
            fs.Read(TempByte, 0, 2);
            ushort NewVal = (ushort)(TempByte[0] + (TempByte[1] * 0x100));
            return NewVal;
        }

        private string ReadString(FileStream fs, int StringLength) {
            // get a string from a passed filestream
            if (StringLength == 0) return ""; // simple error checking
            byte[] TempByte = new byte[StringLength];
            fs.Read(TempByte, 0, StringLength);
            StringBuilder NewStr = new StringBuilder(StringLength);
            for (int CharPos = 0; CharPos < StringLength; CharPos++) {
                NewStr.Append((char)TempByte[CharPos]);
            }
            return NewStr.ToString();
        }

        private void WriteUInt(FileStream fs, uint WriteThis) {
            // write an unsigned int to a passed filestream
            byte[] TempByte = new byte[4];
            uint OldVal = WriteThis;
            TempByte[0] = (byte)(OldVal & 0xFF);
            OldVal >>= 8;
            TempByte[1] = (byte)(OldVal & 0xFF);
            OldVal >>= 8;
            TempByte[2] = (byte)(OldVal & 0xFF);
            OldVal >>= 8;
            TempByte[3] = (byte)OldVal;
            fs.Write(TempByte, 0, 4);
        }
    }
}
