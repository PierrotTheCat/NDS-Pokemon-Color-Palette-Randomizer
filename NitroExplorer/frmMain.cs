﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace NitroExplorer {
    public partial class frmMain : Form {
        public NitroClass ROM;
        private Dictionary<int,TreeNode> DirHolder;
        public int mode;
        public int g;
        public int g1;
        private Random rnd = new Random();
        private string FileName;
        private int lang;
        private bool colorSelected;
        private Color selColor;
        private Color[] Girl;
        private bool[] colorChangedGirl;
        private  Color[] Boy;
        private bool[] colorChangedBoy;
        
        
        public frmMain() {
            InitializeComponent();
            ROM = new NitroClass();
            ROM.ROMIsReady += new NitroClass.ROMIsReadyD(ROM_ROMIsReady);
            ROM.DirReady += new NitroClass.DirReadyD(ROM_DirReady);
            ROM.FileReady += new NitroClass.FileReadyD(ROM_FileReady);
        }

        private void ROM_ROMIsReady(long TimeTakenMS) {
            Single TimeTakenHax = Convert.ToSingle(TimeTakenMS);
            TimeTakenHax /= 1000;
            toolStripStatusLabel1.Text = "ROM loaded."; //Time taken: " + TimeTakenHax.ToString("F2") + " seconds. Mode: "+mode;
            DirHolder[61440].Expand();
        }

        private void ROM_DirReady(int DirID, int ParentID, string DirName, bool IsRoot) {
            if (IsRoot) {
                DirHolder[61440] = tvFiles.Nodes.Add("61440", "Root [" + openFileDialog1.FileName.Substring(openFileDialog1.FileName.LastIndexOf('\\') + 1) + "]", 0, 0);
                DirHolder[61440].Tag = "61440";
            } else {
                DirHolder[DirID] = DirHolder[ParentID].Nodes.Add(DirID.ToString(), DirName, 0, 0);
                DirHolder[DirID].Tag = DirID.ToString();
            }
        }

        private void ROM_FileReady(int FileID, int ParentID, string FileName) {
            DirHolder[ParentID].Nodes.Add(FileID.ToString(), FileName, 2, 2).Tag = FileID.ToString();
        }

        private void aboutButton_Click(object sender, EventArgs e) {
            frmAbout AboutForm = new frmAbout();
            AboutForm.ShowDialog();
            //AboutForm.Dispose();
            this.Activate();
        }

        private void openROMButton_Click(object sender, EventArgs e) {
            if (openFileDialog1.ShowDialog() == DialogResult.OK) {
                toolStripStatusLabel1.Text = "Loading ROM...";
                tvFiles.Nodes.Clear();
                DirHolder = new Dictionary<int,TreeNode>();
                ROM.LoadROM(openFileDialog1.FileName);
                //extractButton.Enabled = false;
                //reinsertButton.Enabled = false;
                
                 FileStream rfs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] TempFile = new byte[16];
            rfs.Read(TempFile, 0, 16);
            rfs.Dispose();
            lang = TempFile[15];	//44 - Deutsch, 45 - Englisch, 46 - Französisch, 53 - Spanisch, A4 - Japanisch, 4B - Koreanisch, 4F - O
            
            g=0;
            g1=-1;
            if((TempFile[8]=='D' && TempFile[9]==0x00) || (TempFile[8]=='P' && TempFile[9]==0x00))	//D - Diamond // P - pearl
            {
            	g=0;
            }
            else if(TempFile[8]=='P' && TempFile[9]=='L')	//PL - platinum
            {
            	g=1;
            }     
            else if((TempFile[8]=='H' && TempFile[9]=='G') || (TempFile[8]=='S' && TempFile[9]=='S'))	//HG - heartgold //SS - soulsilver
            {
            	g=2; 
            	if(TempFile[8]=='S' && TempFile[9]=='S')
            		g1=21;
            }
            else if((TempFile[8]=='B' && TempFile[9]==0x00) || (TempFile[8]=='W' && TempFile[9]==0x00))	//B - black // W - white
            {
            	g=3;
            }
            else if((TempFile[8]=='B' && TempFile[9]=='2') || (TempFile[8]=='W' && TempFile[9]=='2'))	//B2 - black2 // W2 - white2
            {
            	g=4;
            }
            
            
            }
        }

        private void tvFiles_AfterSelect(object sender, TreeViewEventArgs e) {
            ushort FSObjId = Convert.ToUInt16(e.Node.Tag);
            string StatusMsg;
            if (FSObjId >= 61440) {
                StatusMsg = "Directory: " + e.Node.Text + " - ID " + e.Node.Tag;
                //extractButton.Enabled = false;
                //reinsertButton.Enabled = false;
            } else {
                StatusMsg = "Offset: 0x" + ROM.FileOffsets[FSObjId].ToString("X") + " - Size: " + ROM.FileSizes[FSObjId].ToString() + " bytes - ID " + e.Node.Tag;
                //extractButton.Enabled = true;
                //reinsertButton.Enabled = true;
            }
            toolStripStatusLabel1.Text = StatusMsg;
        }

        private void tvFiles_AfterCollapse(object sender, TreeViewEventArgs e) {
            e.Node.ImageIndex = 0;
            e.Node.SelectedImageIndex = 0;
        }

        private void tvFiles_AfterExpand(object sender, TreeViewEventArgs e) {
            e.Node.ImageIndex = 1;
            e.Node.SelectedImageIndex = 1;
        }

        private void extractButton_Click(object sender, EventArgs e) {
            ushort FSObjID = Convert.ToUInt16(tvFiles.SelectedNode.Tag);
            string FileName = ROM.FileNames[FSObjID];
            saveFileDialog1.FileName = FileName;
            if (saveFileDialog1.ShowDialog() == DialogResult.OK) {
                string DestFileName = saveFileDialog1.FileName;
                ExtractFile(FSObjID, DestFileName);
            }
        }

        private void reinsertButton_Click(object sender, EventArgs e) {
            ushort FSObjID = Convert.ToUInt16(tvFiles.SelectedNode.Tag);
            string FileName = ROM.FileNames[FSObjID];
            openFileDialog2.FileName = FileName;
            if (openFileDialog2.ShowDialog() == DialogResult.OK) {
                string SrcFileName = openFileDialog2.FileName;
                ReplaceFile(FSObjID, SrcFileName);
            }
        }

        /* Extract a File */
        private void ExtractFile(ushort FileID, string DestFileName) {
            byte[] TempFile = ROM.ExtractFile(FileID);
            FileStream wfs = new FileStream(DestFileName, FileMode.Create, FileAccess.Write, FileShare.None);
            wfs.Write(TempFile, 0, TempFile.GetLength(0));
            wfs.Dispose();
            toolStripStatusLabel1.Text = "File '" + ROM.FileNames[FileID] + "' extracted successfully.";
        }

        /* Replace a File */
        private void ReplaceFile(ushort FileID, string SrcFileName) {
            FileStream rfs = new FileStream(SrcFileName, FileMode.Open, FileAccess.Read, FileShare.Read);
            byte[] TempFile = new byte[rfs.Length];
            rfs.Read(TempFile, 0, (int)rfs.Length);
            rfs.Dispose();
            ROM.ReplaceFile(FileID, TempFile);
            toolStripStatusLabel1.Text = "File '" + ROM.FileNames[FileID] + "' replaced successfully.";
        }
        
		void ToolStripButton1Click(object sender, EventArgs e)
		{
			frmRandom RandomForm = new frmRandom();
            RandomForm.ShowDialog();
            mode = RandomForm.mode;
            colorSelected = RandomForm.colorSelected;
            if(colorSelected) {
            	selColor = RandomForm.selColor;
            }
            //AboutForm.Dispose();
            this.Activate();            
            
            
            sPoke[] poke;
            if(g<3)
            {            	
            	poke = readPoke1(g,lang);
            }
            else
            {
            	poke = readPoke(g,lang);
            }
            if(mode>0 && mode<5) {
            	ReadBytes(rnd,poke,mode,g,FileName,lang,colorSelected,selColor);
            }
            else if(mode>4 && mode<9) {
            	ReadBytesFamily(rnd,poke,mode,g,FileName,colorSelected,selColor);
            }
            else {
            	ReadBytesTypes(rnd,poke,mode,g,FileName,colorSelected,selColor);
            }
            //toolStripStatusLabel1.Text = "File0: "+file[0].ToString("X")+"File1: "+file[1].ToString("X")+"File1: "+file[2].ToString("X")+"File1: "+file[3].ToString("X");
            toolStripStatusLabel1.Text = "The ROM has been successfully randomized.";
		}
		
		public Color[] newPaletteType(int stage,sPoke[] poke,Color[] palette,int mode,int g,Random rnd,string[] words,bool colorSelected,Color selColor)
		{
			int chanceC = rnd.Next(0,3);
			int mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			
			int j = stage-1;
			int col = poke[j].color;
			//Console.WriteLine("stage: "+stage);
			if(col==73)
			{
				col = 9;
			}
			else if(col==65)
			{
				col=1;
			}
			else if(col==67)
			{
				col=3;
			}
			else if(col==69)
			{
				col=5;
			}
			else if(col==71)
			{
				col=7;
			}
			else if(col==64)
			{
				col=0;
			}
			else if(col==66)
			{
				col=2;
			}
			else if(col==72)
			{
				col=8;
			}
			else if(col==68)
			{
				col=4;
			}
			else if(col==135)
			{
				col=7;
			}
			else if(col==130)
			{
				col=2;
			}
			else if(col==137)
			{
				col=9;
			}
			else if(col==129)
			{
				col=1;
			}
			else if(col==131)
			{
				col=3;
			}
			else if(col==128)
			{
				col=0;
			}
			else if(col==133)
			{
				col=5;
			}
			else if(col==136)
			{
				col=8;
			}
			else if(col==132)
			{
				col=4;
			}
			
			string color = col.ToString();
			Console.WriteLine("color:"+color+" poke j type: "+poke[j].type1+" stage: "+stage);
			
			Color[] c = new Color[9];
			Color[] l = new Color[9];
			Color[] pal = new Color[16];
							
					float[] hue = new float[16];
					float[] sat = new float[16];
					float[] bri = new float[16];
					
					float[] hue2 = new float[16];
					float[] sat2 = new float[16];
					float[] bri2 = new float[16];
					//Console.WriteLine(1);
					int div = 22;
					if(g==0 || g==1 || g==2)
					{
					if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(15*div,16*div); 
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{yyy
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,11*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,5*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,12*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]>0.3);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.5);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,1*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,6*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					//Console.WriteLine("type1: {0}	type2: {1}",poke[j].type1,poke[j].type2);
					//---------
					
						if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(15*div,16*div);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,11*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,5*div);			
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,12*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,1*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,6*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]>0.2);	
						}
					}					
					//Console.WriteLine(3);
					}
					else
					{
						if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(15*div,16*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,11*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,5*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,12*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,1*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,6*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					
					if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(15*div,16*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,11*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,5*div);							
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,12*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,1*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,6*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					}
					//Console.WriteLine(4);
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						if(i%2==1)
						{
							pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
						}
						else
						{
							pal[i] = FromAhsb(255,hue[i],sat[i],bri[i]);
						}
					}
					
					//Console.WriteLine(5);
					//:::::::::::::::::::::::::::::::::::::::::::
					
					
					
					//---------
					
					
					if(sat2[0]>0.9f)
						sat2[0] = sat2[0]*0.9f;
					if(sat[0]>0.9f)
						sat[0] = sat[0]*0.9f;
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=0;i<pal.Length;i++)
					{
						if(sat2[i]>0.9f)
							sat2[i] = sat2[i]*0.9f;
						if(sat[i]>0.9f)
							sat[i] = sat[i]*0.9f;
//						if(bri2[i]>0.9f)
//							bri2[i] = bri2[i]*0.9f;
//						if(bri[i]>0.9f)
//							bri[i] = bri[i]*0.9f;
						
						if(i%2==1)
						{
							pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
						}
						else
						{
							pal[i] = FromAhsb(255,hue[i],sat[i],bri[i]);
						}
					}
					
					/*pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
					}*/
					
					//Console.WriteLine(6);
					/*int random = rnd.Next(0,9);
					pal[0] = c[random];
					for(int i=1;i<pal.Length;i++)
					{
						random = rnd.Next(0,9);
						
						pal[i] = l[random];
						
					}*/
					
					//::::::::::::::::::::::::::::::
					
					
					//first = 0;
					float hu,sa,br;
			        string temp = color;
			        bool[] check = new bool[16];
			        for(int i=1;i<check.Length;i++)
			        {
			        	check[i] = false;
			        }
			        int checksum = 0; 
			        int colorInd = 0;
			        while(checksum<15)
			        {			        	    	
//			        	float hu = (float)(rnd.Next(0, 361));
//						float sa = (float)(rnd.NextDouble());
//						float br = (float)(rnd.NextDouble());
			        	if(checksum>0)
			        	{
			        		hu = pal[colorInd].GetHue();
			        		sa = pal[colorInd].GetSaturation();
			        		br = pal[colorInd].GetBrightness();
			        	}
			        	
			        	if(checksum>0)
			        	{
			        		for(int i=1;i<words.Length;i++)
				        	{
				        		if(check[i] == false)
				        		{
				        			temp = words[i];
				        			break;
				        		}
				        	}	
						}
			        	
			        	int p=0;
			        	for(int s=1;s<words.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			p++;
			        		}
			        	}

			        	/*float[] brightness = new float[p];	
						float[] saturation = new float[p];				        	
			        	int[] index = new int[p];
			        	int q=0;
			        	for(int s=1;s<brightness.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			brightness[q] = palette[0][s].GetBrightness();
			        			saturation[q] = palette[0][s].GetSaturation();
			        			index[q++] = s;
			        		}
			        	}
			        	
			        	float maxValue = brightness.Max();
 						float maxIndex = brightness.ToList().IndexOf(maxValue);
 						
 						float minValue = brightness.Min();
 						float minIndex = brightness.ToList().IndexOf(minValue);
 						
 						int chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 										if(maxValue+br<=1f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] += mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 									if(minValue-br>=0f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] -= mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						} 		

						//SATURATION
						   				        	
			        	maxValue = saturation.Max();
 						maxIndex = saturation.ToList().IndexOf(maxValue);
 						
 						minValue = saturation.Min();
 						minIndex = saturation.ToList().IndexOf(minValue);
 						
 						chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod =0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(maxValue+sa<=1f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] += mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 							 							
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(minValue-sa>=0f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] -= mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 								
 						}*/
			        	
			        	
			        	
			        	
			        	for(int s =1;s<palette.Length;s++)
				        {
				        	if(check[s]==false)
				        	{						

								if(colorSelected==true && chanceC==0 && mainColor==colorInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										
 										sa = pal[colorInd].GetSaturation();
 										if(sa>0.9f)
 											sa = sa*0.9f;
 										
 										pal[colorInd] = FromAhsb(255,hu,sa,pal[colorInd].GetBrightness());
 									}
				        		
					            if(String.Compare(temp,words[s])==0)
					            {		
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	//palette[s] = FromAhsb(255,pal[colorInd].GetHue(),pal[colorInd].GetSaturation(),(pal[colorInd].GetBrightness()*2f+palette[s].GetBrightness()*3f)/5f);
									if((poke[j].color==7 || poke[j].color==8 || poke[j].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
									{
					            		palette[s] = FromAhsb(255,pal[colorInd].GetHue(),(pal[colorInd].GetSaturation()+palette[s].GetSaturation())/2f,(palette[s].GetBrightness()+pal[colorInd].GetBrightness())/2f); //,pal[colorInd].GetSaturation(),(pal[colorInd].GetBrightness()*2f+palette[s].GetBrightness()*3f)/5f);
									}
									checksum++;
									check[s] = true;									
					            }	
				        	}				        								
				        }
 						colorInd++;
			        }
						
					
					/*for(int i=0;i<pal.Length;i++)
						{
							int indC = rnd.Next(0,9);						
														
							pal[i] = c[indC];
												
						}
										
					
					for(int s=1;s<words.Length;s++)
			        {
							
						
							int colInd = Int32.Parse(words[s]);
							
							Console.WriteLine(colInd);
							
							palette[0][s] = FromAhsb(255,pal[colInd].GetHue(),pal[colInd].GetSaturation(),(pal[colInd].GetBrightness()+palette[0][s].GetBrightness()*3)/4);
						
							
				           			        								

					}*/
						return palette;
		}
		
		public Color[] newPaletteTypeMulti(int stage,sPoke[] poke,Color[] palette,int mode,int g,Random rnd,string[] words,bool colorSelected,Color selColor)
		{
			int chanceC = rnd.Next(0,3);
			int mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			
			int j = stage-1;
			int col = poke[j].color;
			//Console.WriteLine("stage: "+stage);
			if(col==73)
			{
				col = 9;
			}
			else if(col==65)
			{
				col=1;
			}
			else if(col==67)
			{
				col=3;
			}
			else if(col==69)
			{
				col=5;
			}
			else if(col==71)
			{
				col=7;
			}
			else if(col==64)
			{
				col=0;
			}
			else if(col==66)
			{
				col=2;
			}
			else if(col==72)
			{
				col=8;
			}
			else if(col==68)
			{
				col=4;
			}
			else if(col==135)
			{
				col=7;
			}
			else if(col==130)
			{
				col=2;
			}
			else if(col==137)
			{
				col=9;
			}
			else if(col==129)
			{
				col=1;
			}
			else if(col==131)
			{
				col=3;
			}
			else if(col==128)
			{
				col=0;
			}
			else if(col==133)
			{
				col=5;
			}
			else if(col==136)
			{
				col=8;
			}
			else if(col==132)
			{
				col=4;
			}
			
			string color = col.ToString();
			Console.WriteLine("color:"+color+" poke j type: "+poke[j].type1+" stage: "+stage);
			
			Color[] c = new Color[9];
			Color[] l = new Color[9];
			Color[] pal = new Color[16];
							
					float[] hue = new float[16];
					float[] sat = new float[16];
					float[] bri = new float[16];
					
					float[] hue2 = new float[16];
					float[] sat2 = new float[16];
					float[] bri2 = new float[16];
					//Console.WriteLine(1);
					int div = 22;
					if(g==0 || g==1 || g==2)
					{
					if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,361); 
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{yyy
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,12*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(3*div,6*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,11*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]>0.3);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.5);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							int chance = rnd.Next(0,2);
							if(chance==0)
								hue[i] = rnd.Next(0*div,2*div);
							else
								hue[i] = rnd.Next(16*div,361);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,4*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,16*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					//Console.WriteLine("type1: {0}	type2: {1}",poke[j].type1,poke[j].type2);
					//---------
					
						if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,3*div);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,361);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,12*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(3*div,6*div);			
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,11*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue2.Length;i++)
						{
							int chance = rnd.Next(0,2);
							if(chance==0)
								hue2[i] = rnd.Next(0*div,1*div);
							else
								hue2[i] = rnd.Next(16*div,361);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,4*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,16*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]>0.2);	
						}
					}					
					//Console.WriteLine(3);
					}
					else
					{
						if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,4*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,361);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,12*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(3*div,6*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,11*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							int chance = rnd.Next(0,2);
							if(chance==0)
								hue[i] = rnd.Next(0*div,2*div);
							else
								hue[i] = rnd.Next(16*div,361);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,4*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,16*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					
					if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,4*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,361);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,12*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(3*div,6*div);							
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,11*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							int chance = rnd.Next(0,2);
							if(chance==0)
								hue2[i] = rnd.Next(0*div,2*div);
							else
								hue2[i] = rnd.Next(16*div,361);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,4*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,16*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					}
					//Console.WriteLine(4);
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						if(i%2==1)
						{
							pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
						}
						else
						{
							pal[i] = FromAhsb(255,hue[i],sat[i],bri[i]);
						}
					}
					
					//Console.WriteLine(5);
					//:::::::::::::::::::::::::::::::::::::::::::
					
					
					
					//---------
					
					
					if(sat2[0]>0.9f)
						sat2[0] = sat2[0]*0.9f;
					if(sat[0]>0.9f)
						sat[0] = sat[0]*0.9f;
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=0;i<pal.Length;i++)
					{
						if(sat2[i]>0.9f)
							sat2[i] = sat2[i]*0.9f;
						if(sat[i]>0.9f)
							sat[i] = sat[i]*0.9f;
//						if(bri2[i]>0.9f)
//							bri2[i] = bri2[i]*0.9f;
//						if(bri[i]>0.9f)
//							bri[i] = bri[i]*0.9f;
						
						if(i%2==1)
						{
							pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
						}
						else
						{
							pal[i] = FromAhsb(255,hue[i],sat[i],bri[i]);
						}
					}
					
					/*pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
					}*/
					
					//Console.WriteLine(6);
					/*int random = rnd.Next(0,9);
					pal[0] = c[random];
					for(int i=1;i<pal.Length;i++)
					{
						random = rnd.Next(0,9);
						
						pal[i] = l[random];
						
					}*/
					
					//::::::::::::::::::::::::::::::
					
					
					//first = 0;
					float hu,sa,br;
			        string temp = color;
			        bool[] check = new bool[16];
			        for(int i=1;i<check.Length;i++)
			        {
			        	check[i] = false;
			        }
			        int checksum = 0; 
			        int colorInd = 0;
			        while(checksum<15)
			        {			        	    	
//			        	float hu = (float)(rnd.Next(0, 361));
//						float sa = (float)(rnd.NextDouble());
//						float br = (float)(rnd.NextDouble());
			        	if(checksum>0)
			        	{
			        		hu = pal[colorInd].GetHue();
			        		sa = pal[colorInd].GetSaturation();
			        		br = pal[colorInd].GetBrightness();
			        	}
			        	
			        	if(checksum>0)
			        	{
			        		for(int i=1;i<words.Length;i++)
				        	{
				        		if(check[i] == false)
				        		{
				        			temp = words[i];
				        			break;
				        		}
				        	}	
						}
			        	
			        	int p=0;
			        	for(int s=1;s<words.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			p++;
			        		}
			        	}

			        	/*float[] brightness = new float[p];	
						float[] saturation = new float[p];				        	
			        	int[] index = new int[p];
			        	int q=0;
			        	for(int s=1;s<brightness.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			brightness[q] = palette[0][s].GetBrightness();
			        			saturation[q] = palette[0][s].GetSaturation();
			        			index[q++] = s;
			        		}
			        	}
			        	
			        	float maxValue = brightness.Max();
 						float maxIndex = brightness.ToList().IndexOf(maxValue);
 						
 						float minValue = brightness.Min();
 						float minIndex = brightness.ToList().IndexOf(minValue);
 						
 						int chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 										if(maxValue+br<=1f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] += mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 									if(minValue-br>=0f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] -= mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						} 		

						//SATURATION
						   				        	
			        	maxValue = saturation.Max();
 						maxIndex = saturation.ToList().IndexOf(maxValue);
 						
 						minValue = saturation.Min();
 						minIndex = saturation.ToList().IndexOf(minValue);
 						
 						chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod =0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(maxValue+sa<=1f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] += mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 							 							
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(minValue-sa>=0f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] -= mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 								
 						}*/
			        	
			        	
			        	
			        	
			        	for(int s =1;s<palette.Length;s++)
				        {
				        	if(check[s]==false)
				        	{						

								if(colorSelected==true && chanceC==0 && mainColor==colorInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										
 										sa = pal[colorInd].GetSaturation();
 										if(sa>0.9f)
 											sa = sa*0.9f;
 										
 										pal[colorInd] = FromAhsb(255,hu,sa,pal[colorInd].GetBrightness());
 									}
				        		
					            if(String.Compare(temp,words[s])==0)
					            {		
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	if((poke[j].color==7 || poke[j].color==8 || poke[j].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
					            	palette[s] = FromAhsb(255,pal[colorInd].GetHue(),(pal[colorInd].GetSaturation()+palette[s].GetSaturation())/2f,(palette[s].GetBrightness()+pal[colorInd].GetBrightness())/2f); //,pal[colorInd].GetSaturation(),(pal[colorInd].GetBrightness()*2f+palette[s].GetBrightness()*3f)/5f);
					            	}
					            	checksum++;
									check[s] = true;									
					            }	
				        	}				        								
				        }
 						colorInd++;
			        }
						
					
					/*for(int i=0;i<pal.Length;i++)
						{
							int indC = rnd.Next(0,9);						
														
							pal[i] = c[indC];
												
						}
										
					
					for(int s=1;s<words.Length;s++)
			        {
							
						
							int colInd = Int32.Parse(words[s]);
							
							Console.WriteLine(colInd);
							
							palette[0][s] = FromAhsb(255,pal[colInd].GetHue(),pal[colInd].GetSaturation(),(pal[colInd].GetBrightness()+palette[0][s].GetBrightness()*3)/4);
						
							
				           			        								

					}*/
						return palette;
		}
		
		public Color[] newPaletteTypeOne(int stage,sPoke[] poke,Color[] palette,int mode,int g,Random rnd,string[] words,bool colorSelected,Color selColor)
		{
			int chanceC = rnd.Next(0,3);
			int mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			int mainColor2;
			
			
			int j = stage-1;
			int col = poke[j].color;
			//Console.WriteLine("stage: "+stage);
			if(col==73)
			{
				col = 9;
			}
			else if(col==65)
			{
				col=1;
			}
			else if(col==67)
			{
				col=3;
			}
			else if(col==69)
			{
				col=5;
			}
			else if(col==71)
			{
				col=7;
			}
			else if(col==64)
			{
				col=0;
			}
			else if(col==66)
			{
				col=2;
			}
			else if(col==72)
			{
				col=8;
			}
			else if(col==68)
			{
				col=4;
			}
			else if(col==135)
			{
				col=7;
			}
			else if(col==130)
			{
				col=2;
			}
			else if(col==137)
			{
				col=9;
			}
			else if(col==129)
			{
				col=1;
			}
			else if(col==131)
			{
				col=3;
			}
			else if(col==128)
			{
				col=0;
			}
			else if(col==133)
			{
				col=5;
			}
			else if(col==136)
			{
				col=8;
			}
			else if(col==132)
			{
				col=4;
			}
			
			string color = col.ToString();
			Console.WriteLine("color:"+color+" poke j type: "+poke[j].type1+" stage: "+stage);
			int v=0;
			do{
				mainColor2 = Int32.Parse(words[rnd.Next(1,16)]);
				v++;
			}while(col.Equals(mainColor2) && v<15);
			string mainCol2 = mainColor2.ToString();
			
			Color[] c = new Color[9];
			Color[] l = new Color[9];
			Color[] pal = new Color[16];
							
					float[] hue = new float[16];
					float[] sat = new float[16];
					float[] bri = new float[16];
					
					float[] hue2 = new float[16];
					float[] sat2 = new float[16];
					float[] bri2 = new float[16];
					//Console.WriteLine(1);
					int div = 22;
					if(g==0 || g==1 || g==2)
					{
					if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(15*div,16*div); 
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,11*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,5*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,12*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]>0.3);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.5);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,1*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,6*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					//Console.WriteLine("type1: {0}	type2: {1}",poke[j].type1,poke[j].type2);
					//---------
					
						if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(15*div,16*div);
							
							do{
							
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,11*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,5*div);			
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,12*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//fire
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,1*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//water
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//grass
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,6*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//electric
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//psychic
					{
						for(int i=0;i<hue2.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(17)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]>0.2);	
						}
					}					
					//Console.WriteLine(3);
					}
					else
					{
						if(poke[j].type1.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}						
					}
					else if(poke[j].type1.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(15*div,16*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}	
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue[i] = rnd.Next(0,11); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);
//							}
//							else
//							{
//								hue[i] = rnd.Next(349,361); 
//								do{
//									sat[i] = (float)rnd.NextDouble();
//								}while(sat[i]<0.2);
//								do{
//									bri[i] = (float)rnd.NextDouble();
//								}while(bri[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type1.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(10*div,11*div);						
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(12*div,13*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}						
					}
					else if(poke[j].type1.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(1*div,2*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);								
						}	
					}
					else if(poke[j].type1.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(4*div,5*div);							
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);						
						}
					}
					else if(poke[j].type1.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(11*div,12*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);
						}
					}
					else if(poke[j].type1.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(9*div,10*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(0*div,1*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(8*div,9*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(5*div,6*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(13*div,14*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(7*div,8*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(6*div,7*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					else if(poke[j].type1.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue[i] = rnd.Next(14*div,15*div);
							do{
								sat[i] = (float)rnd.NextDouble();
							}while(sat[i]<0.2);
							do{
								bri[i] = (float)rnd.NextDouble();
							}while(bri[i]<0.2);	
						}
					}
					
					if(poke[j].type2.CompareTo(0)==0)	//normal
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(2*div,(int)(2*div+3*div)/2-3);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}						
					}
					else if(poke[j].type2.CompareTo(1)==0)	//fighting
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(15*div,16*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
						
//						int chance;
//						for(int i=0;i<hue.Length;i++)
//						{
//							chance = rnd.Next(0,2);
//							if(chance==0)
//							{
//								hue2[i] = rnd.Next(0,11); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);
//							}
//							else
//							{
//								hue2[i] = rnd.Next(349,361); 
//								do{
//									sat2[i] = (float)rnd.NextDouble();
//								}while(sat2[i]<0.2);
//								do{
//									bri2[i] = (float)rnd.NextDouble();
//								}while(bri2[i]<0.2);							
//							}
//						}
					}
					else if(poke[j].type2.CompareTo(2)==0)	//flying
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(10*div,11*div);						
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(3)==0)	//poison
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(12*div,13*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(4)==0)	//ground
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}						
					}
					else if(poke[j].type2.CompareTo(5)==0)	//rock
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(1*div,2*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);								
						}	
					}
					else if(poke[j].type2.CompareTo(6)==0)	//bug
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(4*div,5*div);							
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);						
						}
					}
					else if(poke[j].type2.CompareTo(7)==0)	//ghost
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(11*div,12*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);
						}
					}
					else if(poke[j].type2.CompareTo(8)==0)	//steel
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(9*div,10*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(9)==0)	//fire
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(0*div,1*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(10)==0)	//water
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(8*div,9*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(11)==0)	//grass
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(5*div,6*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(12)==0)	//electric
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next((int)(2*div+3*div)/2-3,3*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(13)==0)	//psychic
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(13*div,14*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(14)==0)	//ice
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(7*div,8*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(15)==0)	//dragon
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(6*div,7*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					else if(poke[j].type2.CompareTo(16)==0)	//dark
					{
						for(int i=0;i<hue.Length;i++)
						{
							hue2[i] = rnd.Next(14*div,15*div);
							do{
								sat2[i] = (float)rnd.NextDouble();
							}while(sat2[i]<0.2);
							do{
								bri2[i] = (float)rnd.NextDouble();
							}while(bri2[i]<0.2);	
						}
					}
					}
					//Console.WriteLine(4);
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
					}
					
					//Console.WriteLine(5);
					//:::::::::::::::::::::::::::::::::::::::::::
					
					
					
					//---------
					
					
					
					
					if(sat2[0]>0.9f)
							sat2[0] = sat2[0]*0.9f;
						if(sat[0]>0.9f)
							sat[0] = sat[0]*0.9f;
//						if(bri2[0]>0.9f)
//							bri2[0] = bri2[0]*0.9f;
//						if(bri[0]>0.9f)
//							bri[0] = bri[0]*0.9f;
						
					
					pal[0] = FromAhsb(255,hue[0],sat[0],bri[0]);
					for(int i=1;i<pal.Length;i++)
					{
						if(sat2[i]>0.9f)
							sat2[i] = sat2[i]*0.9f;
						if(sat[i]>0.9f)
							sat[i] = sat[i]*0.9f;
//						if(bri2[i]>0.9f)
//							bri2[i] = bri2[i]*0.9f;
//						if(bri[i]>0.9f)
//							bri[i] = bri[i]*0.9f;
						
						pal[i] = FromAhsb(255,hue2[i],sat2[i],bri2[i]);
					}
					
					//Console.WriteLine(6);
					/*int random = rnd.Next(0,9);
					pal[0] = c[random];
					for(int i=1;i<pal.Length;i++)
					{
						random = rnd.Next(0,9);
						
						pal[i] = l[random];
						
					}*/
					
					//::::::::::::::::::::::::::::::
					
					
					//first = 0;
					float hu,sa,br;
			        string temp = color; 
			        bool[] check = new bool[16];
			        for(int i=1;i<check.Length;i++)
			        {
			        	check[i] = false;
			        }
			        int checksum = 0; 
			        int colorInd = 0;
			        while(checksum<15)
			        {			        	    	
//			        	float hu = (float)(rnd.Next(0, 361));
//						float sa = (float)(rnd.NextDouble());
//						float br = (float)(rnd.NextDouble());
			        	if(checksum>0)
			        	{
			        		hu = pal[colorInd].GetHue();
			        		sa = pal[colorInd].GetSaturation();
			        		br = pal[colorInd].GetBrightness();
			        	}
			        	
			        	if(checksum>0)
			        	{
			        		for(int i=1;i<words.Length;i++)
				        	{
				        		if(check[i] == false)
				        		{
				        			temp = words[i];
				        			break;
				        		}
				        	}	
						}
			        	
			        	int p=0;
			        	for(int s=1;s<words.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			p++;
			        		}
			        	}

			        	/*float[] brightness = new float[p];	
						float[] saturation = new float[p];				        	
			        	int[] index = new int[p];
			        	int q=0;
			        	for(int s=1;s<brightness.Length;s++)
			        	{
			        		if(String.Compare(temp,words[s])==0)
					        {	
			        			brightness[q] = palette[0][s].GetBrightness();
			        			saturation[q] = palette[0][s].GetSaturation();
			        			index[q++] = s;
			        		}
			        	}
			        	
			        	float maxValue = brightness.Max();
 						float maxIndex = brightness.ToList().IndexOf(maxValue);
 						
 						float minValue = brightness.Min();
 						float minIndex = brightness.ToList().IndexOf(minValue);
 						
 						int chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 										if(maxValue+br<=1f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] += mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<be.Length;o++)
// 								{ 									
 									
 									if(minValue-br>=0f)
	 									{
	 										mod = br;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<brightness.Length;s++)
 								{
 									brightness[s] -= mod;
 									System.Console.WriteLine("brightness: "+brightness[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),palette[0][index[s]].GetSaturation(),brightness[s]);
 								} 								
 														
 						} 		

						//SATURATION
						   				        	
			        	maxValue = saturation.Max();
 						maxIndex = saturation.ToList().IndexOf(maxValue);
 						
 						minValue = saturation.Min();
 						minIndex = saturation.ToList().IndexOf(minValue);
 						
 						chance = rnd.Next(0,2);
 						
 						if(chance==0)	//plus
 						{
 							
 								float mod =0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(maxValue+sa<=1f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] += mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 							 							
 						}
 						else	//minus
 						{
 							
 								float mod = 0f;
 								
// 								for(int o=0;o<sa.Length;o++)
// 								{ 									
 									
 									if(minValue-sa>=0f)
	 									{
	 										mod = sa;
//	 										break;
	 									}
 									
//		 						}
 								
 								for(int s=0;s<saturation.Length;s++)
 								{
 									saturation[s] -= mod;
 									System.Console.WriteLine("saturation: "+saturation[s]);
 									palette[0][index[s]] = FromAhsb(255,palette[0][index[s]].GetHue(),saturation[s],palette[0][index[s]].GetBrightness());
 								} 								
 								
 						}*/
			        	
			        	
			        	
			        	
			        	for(int s =1;s<palette.Length;s++)
				        {
				        	if(check[s]==false)
				        	{						

								if(colorSelected==true && chanceC==0 && mainColor==colorInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hu = (palette[s].GetHue()+diff)%360f;
 										}
 										
 										sa = pal[colorInd].GetSaturation();
 										if(sa>0.9f)
 											sa = sa*0.9f;
 										
 										pal[colorInd] = FromAhsb(255,hu,sa,pal[colorInd].GetBrightness());
 									}
				        		
								
						            if(String.Compare(temp,words[s])==0)
						            {		
						            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
						            	if(colorInd==0)
						            	{
						            		if((poke[j].color==7 || poke[j].color==8 || poke[j].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
						            		palette[s] = FromAhsb(255,pal[colorInd].GetHue(),pal[colorInd].GetSaturation(),(pal[colorInd].GetBrightness()*2f+palette[s].GetBrightness()*3f)/5f);
						            		}
						            		}
										checksum++;
										check[s] = true;									
						            }
						           
						            if(poke[j].type1.CompareTo(poke[j].type2)!=0)	//wenn beide Pokemontypen nicht gleich
						            {
						            	if(String.Compare(mainCol2,words[s])==0)
							            {		
							            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
							            	if(colorInd==1)
							            	{
							            		if((poke[j].color==7 || poke[j].color==8 || poke[j].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
							            		palette[s] = FromAhsb(255,pal[colorInd].GetHue(),pal[colorInd].GetSaturation(),(pal[colorInd].GetBrightness()*2+palette[s].GetBrightness()*3f)/5f);
							            		}
							            		}
																				
							            }
						            }
								
				        	}				        								
				        }
 						colorInd++;
			        }
						
					
					/*for(int i=0;i<pal.Length;i++)
						{
							int indC = rnd.Next(0,9);						
														
							pal[i] = c[indC];
												
						}
										
					
					for(int s=1;s<words.Length;s++)
			        {
							
						
							int colInd = Int32.Parse(words[s]);
							
							Console.WriteLine(colInd);
							
							palette[0][s] = FromAhsb(255,pal[colInd].GetHue(),pal[colInd].GetSaturation(),(pal[colInd].GetBrightness()+palette[0][s].GetBrightness()*3)/4);
						
							
				           			        								

					}*/
						return palette;
		}
		
		public int ReadBytesTypes(Random rnd,sPoke[] poke, int mode,int g,string file,bool colorSelected,Color selColor)
		{
			//byte[] game; 
			
			//int j = 110422356; //first data bit of Bulbasaur //60 Bytes until Bisaknosp
								//40 Bytes in use
		
				//Offset a/0/0/4 = 59650560 
				//Offset Bulbasaur NCLR = 059650560 + 123572 = 59774132
					//Offset palette = Offset Bulbasaur NCLR + 40 Byte = 59774172
					//Length palette = 32 Byte
				//Length of one NCLR: 72 bytes 
			
				string[] lines;
			        if(g==0 || g==1)
			        {
			        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
			        }
			        else
			        {
			        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
			        }
					char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

					
				byte[] game;
				if(g==0) 	//diamond
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(337);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(317);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(338);
					}
				}
				else if(g==1)	//platinum
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(434);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(409);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(440);
					}
				}
				else if(g==2)	//heartgold
				{
					if(lang==0x45 || lang==0x4B) {
						 game = ROM.ExtractFile(133);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(133);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(132);
					}
				}
				else if(g==3)	//black
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(246);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(246);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(246);
					}
				}
				else //if(g==4)	//black2
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(351);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(351);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(351);
					}
				}
				
				//int offsetNarc = 0;
				int l=1;
				int i = 59774131;	//offset bulbasaur a004 59650560 + file39 123572
				int last;
				
					
					int firstData = 0;
					int counter = 0;
					for(int k=0;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					i = firstData;
					
					if(g<3)
					{
						i = 0x126E4;
						last = 491;
					}
					else if(g==3)
					{
						i = 0x1E2B4;
						last = 649;
					}
					else
					{
						i = 0x1FB14;
						last = 649;
					}
				
				byte[] bytearray = new byte[32];
				int length = game.Length;
				int[] offset = new int[last];
				Color[,] pal = new Color[last,16];
				for(;l<last;i++)
				{
					if(game[i]==0x52 && game[i+1]==0x4C && game[i+2]==0x43 && game[i+3]==0x4E)
					{
						string[] words = lines[l].Split(delimiterChars);
						
						offset[l] = i+40;
						
						for(int p=offset[l];p<offset[l]+32;p++)
						{
							bytearray[p-offset[l]] = game[p];
						}
						Color[] palette = Actions.BGR555ToColor(bytearray);
												
//						for(int k=0;k<palette.Length;k++)
//						{
//							Console.WriteLine("Palette: {0}	Color: {1}",l,pal[l,k]);
//						}
						
						if(mode==9)
						{  
							palette = newPaletteType(l,poke,palette,mode,g,rnd,words,colorSelected,selColor);
							
						}
						else if(mode==10)
						{
							palette = newPaletteTypeOne(l,poke,palette,mode,g,rnd,words,colorSelected,selColor);
						}
						else
						{
							palette = newPaletteTypeMulti(l,poke,palette,mode,g,rnd,words,colorSelected,selColor);
						}
							
						 
						for(int m=0;m<palette.Length;m++)
						{
							pal[l,m] = palette[m];
						}						
						
						bytearray = Actions.ColorToBGR555(palette);
						
						for(int p=offset[l];p<offset[l]+32;p++)
						{
							game[p] = bytearray[p-offset[l]];
						}
						
											
						i = i+75;
						l++;
					}
										
				}
				
				/*if(g==1)	//platinum
				{
					l=0;
					if(random==false)
					{
						i= 44056576+75492;	//platinum pokegra
					}
					else
					{
						i=86027776+75492;
					}
					for(;l<last;i++)
					{
						if(game[i]==0x52 && game[i+1]==0x4C && game[i+2]==0x43 && game[i+3]==0x4E)
						{
							offset[l] = i+40;
							
							for(int p=offset[l];p<offset[l]+32;p++)
							{
								bytearray[p-offset[l]] = game[p];
							}
							
							Color[] palette = new Color[16];
							for(int m=0;m<palette.Length;m++)
							{
								palette[m] = pal[l,m];
							}
																																
							bytearray = Actions.ColorToBGR555(palette);
							
							for(int p=offset[l];p<offset[l]+32;p++)
							{
								game[p] = bytearray[p-offset[l]];
							}
							
												
							i = i+75;
							
							l++;
							
							
						}
											
					}
				}*/
				
				if(g==0)
				{
					if(lang==0x45) {
						ROM.ReplaceFile(337,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(317,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(338,game);
					}
				}
				else if(g==1) 
				{
					if(lang==0x45) {
						ROM.ReplaceFile(434,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(409,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(440,game);
					}
				}
				else if(g==2) 
				{
					if(lang==0x45 || lang==0x4B) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(132,game);
					}
				}
				else if(g==3) 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(246,game);
					}
				}
				else if(g==4) 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(351,game);
					}
				}
				
//				Console.WriteLine("Writing...");
//				int position = file.LastIndexOf('.');
//				string f = file.Substring(0,position);
//				string path = f+"Random.nds";
//				Console.WriteLine(path);
//				File.WriteAllBytes(path,game);
				
//				if(g==4)
//				{
//					File.WriteAllBytes("Black2Random.nds",game);
//				}
//				else if(g==3)
//				{
//					File.WriteAllBytes("BlackRandom.nds",game);
//				}
//				else if(g==2)
//				{
//					File.WriteAllBytes("HeartGoldRandom.nds",game);
//				}
//				else if(g==1)
//				{
//					File.WriteAllBytes("PlatinumRandom.nds",game);
//				}
//				else if(g==0)
//				{
//					File.WriteAllBytes("DiamondRandom.nds",game);
//				}
				
//				palette = Actions.BGR555ToColor(pal);
//				
//				for(i=0;i<palette.Length;i++)
//				{
//					Console.WriteLine("R: {0} G: {1} B: {2}",palette[i].R,palette[i].G,palette[i].B);
//				}
				
			return 0;
		}
		
		
		public struct sPoke      // Nintendo CoLor Resource
		{
			public byte basehp;
			public byte baseatk;
			public byte basedef;
			public byte	basespeed;
			public byte basespatk;
			public byte basespdef;
			public byte type1;
			public byte type2;
			public byte catchrate;
			public byte stage;
			public ushort evs;
			public ushort item1;
			public ushort item2;
			public ushort item3;
			public byte	gender;
			public byte hatchcycle;
			public byte basehappy;
			public byte exprate;
			public byte egggroup1;
			public byte egggroup2;
			public byte ability1;
			public byte ability2;
			public byte ability3;
			public byte flee;
			public ushort formid;
			public ushort form;
			public byte numforms;
			public byte color;
			public ushort baseexp;
			public ushort height;
			public ushort weight;
		}
		
		public static class Actions
	    {
	        public static Color[] BGR555ToColor(byte[] bytes)
	        {
	            Color[] colors = new Color[bytes.Length / 2];
	
	            for (int i = 0; i < bytes.Length / 2; i++)
	                colors[i] = BGR555ToColor(bytes[i * 2], bytes[i * 2 + 1]);
	
	            return colors;
	        }
		
			//------------------------------
			
			public static Color BGR555ToColor(byte byte1, byte byte2)
	        {
	            int r, b, g;
	            short bgr = BitConverter.ToInt16(new Byte[] { byte1, byte2 }, 0);
	
	            r = (bgr & 0x001F) * 0x08;
	            g = ((bgr & 0x03E0) >> 5) * 0x08;
	            b = ((bgr & 0x7C00) >> 10) * 0x08;
	
	            return Color.FromArgb(r, g, b);
	        }
			
			//------------------------------
			
			public static Byte[] ColorToBGR555(Color[] colors)
	        {
	            byte[] data = new byte[colors.Length * 2];
	
	            for (int i = 0; i < colors.Length; i++)
	            {
	                byte[] bgr = ColorToBGR555(colors[i]);
	                data[i * 2] = bgr[0];
	                data[i * 2 + 1] = bgr[1];
	            }
	
	            return data;
	        }
			
			//------------------------------
			
			public static Byte[] ColorToBGR555(Color color)
	        {
	            byte[] d = new byte[2];
	
	            int r = color.R / 8;
	            int g = (color.G / 8) << 5;
	            int b = (color.B / 8) << 10;
	
	            ushort bgr = (ushort)(r + g + b);
	            Array.Copy(BitConverter.GetBytes(bgr), d, 2);
	
	            return d;
	        }
		}
		
		public static ushort Combine16(byte b1, byte b2)
		{
			ushort combined = (ushort)(b1 << 8 | b2);
		    return combined;
		}
		
		private sPoke[] readPoke(int g,int lang)
		{
			sPoke[] poke = new sPoke[669];
			uint j=175543984;
			//int offsetNarc = 0;
			byte[] game = new byte[poke.Length];
			
			if(g==3)				//black
				{
					//a/0/1/6
					if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {	//deutsch oder französisch oder spanisch
						game = ROM.ExtractFile(258);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x45 || lang==0x4B || lang==0x4F) {			//englisch oder koreanisch
						game = ROM.ExtractFile(258);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x4A) 			//japanisch
					{
						game = ROM.ExtractFile(258);
						j = ROM.FileOffsets[326];
					}
           		
           			uint firstData = 0x1554;
					j = firstData;
				}
				else if(g==4)	//black2
				{
					//poketool/personal/personal
					//j = 4160;
					//poketool/pokegra/pokegra
					//a/0/1/6
					if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {	//deutsch oder französisch oder spanisch
						game = ROM.ExtractFile(363);
						//j = ROM.FileOffsets[326];
					}
					else if(lang==0x45 || lang==0x4B || lang==0x4F) {			//englisch oder koreanisch
						game = ROM.ExtractFile(363);
						//j = ROM.FileOffsets[326];
					}
					else if(lang==0x4A) 			//japanisch
					{
						game = ROM.ExtractFile(363);
						//j = ROM.FileOffsets[326];
					}
           		
           			uint firstData = 0x16B0;
					j = firstData;
				}				
			
			
			for(int i=0;i<poke.Length;i++)
			{
				poke[i].basehp = game[j];j++;							//Console.WriteLine("basehp: "+poke[i].basehp);
				poke[i].baseatk = game[j];j++;							//Console.WriteLine("baseatk: "+poke[i].baseatk);
				poke[i].basedef = game[j];j++;							//Console.WriteLine("basedef: "+poke[i].basedef);
				poke[i].basespeed = game[j];j++;						//Console.WriteLine("basespeed: "+poke[i].basespeed);
				poke[i].basespatk = game[j];j++;						//Console.WriteLine("basespatk: "+poke[i].basespatk);
				poke[i].basespdef = game[j];j++;						//Console.WriteLine("basespdef: "+poke[i].basespdef);
				poke[i].type1 = game[j];j++;							//Console.WriteLine("type1: "+poke[i].type1);
				poke[i].type2 = game[j];j++;							//Console.WriteLine("type2: "+poke[i].type2);
				poke[i].catchrate = game[j];j++;						//Console.WriteLine("catchrate: "+poke[i].catchrate);
				poke[i].stage = game[j];j++;							//Console.WriteLine("stage: "+poke[i].stage);
				poke[i].evs = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("evs: "+poke[i].evs);
				poke[i].item1 = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("item1: "+poke[i].item1);
				poke[i].item2 = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("item2: "+poke[i].item2);
				poke[i].item3 = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("item3: "+poke[i].item3);
				poke[i].gender = game[j];j++;							//Console.WriteLine("gender: "+poke[i].gender);
				poke[i].hatchcycle = game[j];j++;						//Console.WriteLine("hatchcycle: "+poke[i].hatchcycle);
				poke[i].basehappy = game[j];j++;						//Console.WriteLine("basehappy: "+poke[i].basehappy);
				poke[i].exprate = game[j];j++;							//Console.WriteLine("exprate: "+poke[i].exprate);
				poke[i].egggroup1 = game[j];j++;						//Console.WriteLine("egggroup1: "+poke[i].egggroup1);
				poke[i].egggroup2 = game[j];j++;						//Console.WriteLine("egggroup2: "+poke[i].egggroup2);
				poke[i].ability1 = game[j];j++;							//Console.WriteLine("ability1: "+poke[i].ability1);
				poke[i].ability2 = game[j];j++;							//Console.WriteLine("ability2: "+poke[i].ability2);
				poke[i].ability3 = game[j];j++;							//Console.WriteLine("ability3: "+poke[i].ability3);
				poke[i].flee = game[j];j++;								//Console.WriteLine("flee: "+poke[i].flee);
				poke[i].formid = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("formid: "+poke[i].formid);
				poke[i].form = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("form: "+poke[i].form);
				poke[i].numforms = game[j];j++;							//Console.WriteLine("numforms: "+poke[i].numforms);
				poke[i].color = game[j];j++;							//Console.WriteLine("color: "+poke[i].color);
				poke[i].baseexp = Combine16(game[j+1],game[j]);j+=2;	//Console.WriteLine("basexp: "+poke[i].baseexp);
				poke[i].height = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("height: "+poke[i].height);
				poke[i].weight = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("weight: "+poke[i].weight);Console.WriteLine(" ");
				
				if(g==3)	//Black
				{
					j+=20;
				}
				else
				{
					j+=36;
				}
			}
			return poke;
			
			
			
//			string[] filePaths = Directory.GetFiles(".\\a016_black2");
//			sPoke[] poke = new sPoke[filePaths.Length];
//			
//			foreach (string file in filePaths)
//		    {
//				int position = file.LastIndexOf('\\');
//				string s = file.Substring(position + 1);
//				if(s.Length<9)
//				{
//					s = reString(s);
//					string temppath = Path.Combine(".\\a016_black2", s);
//		        	//file.CopyTo(temppath, true);
//		        	System.IO.File.Move(file,temppath);
//				}
//			}
//			
//			for(int i=0;i<filePaths.Length;i++)
//			{
//				//Console.WriteLine(filePaths[i]);
//				Stream file = File.Open(filePaths[i],FileMode.Open);
//			
//				//Stream file = File.Open("D:\\PK\\Black2\\6\\file4",FileMode.Open);
//				byte[] stat = ReadAllBytes(file);
//								
//				poke[i].basehp = stat[0];	//Console.WriteLine("basehp: "+poke[i].basehp);	
//				poke[i].baseatk = stat[1];	//Console.WriteLine("baseatk: "+poke[i].baseatk);
//				poke[i].basedef = stat[2];	//Console.WriteLine("basedef: "+poke[i].basedef);
//				poke[i].basespeed = stat[3];	//Console.WriteLine("basespeed: "+poke[i].basespeed);
//				poke[i].basespatk = stat[4];	//Console.WriteLine("basespatk: "+poke[i].basespatk);
//				poke[i].basespdef = stat[5];	//Console.WriteLine("basespdef: "+poke[i].basespdef);
//				poke[i].type1 = stat[6];		//Console.WriteLine("type1: "+poke[i].type1);
//				poke[i].type2 = stat[7];		//Console.WriteLine("type2: "+poke[i].type2);
//				poke[i].catchrate = stat[8];	//Console.WriteLine("catchrate: "+poke[i].catchrate);
//				poke[i].stage = stat[9];		//Console.WriteLine("stage: "+poke[i].stage);
//				poke[i].evs = Combine16(stat[11],stat[10]);		//Console.WriteLine("evs: "+poke[i].evs);
//				poke[i].item1 = Combine16(stat[13],stat[12]);	//Console.WriteLine("item1: "+poke[i].item1);
//				poke[i].item2 = Combine16(stat[15],stat[14]);	//Console.WriteLine("item2: "+poke[i].item2);
//				poke[i].item3 = Combine16(stat[17],stat[16]);	//Console.WriteLine("item3: "+poke[i].item3);
//				poke[i].gender = stat[18];	//Console.WriteLine("gender: "+poke[i].gender);
//				poke[i].hatchcycle = stat[19];	//Console.WriteLine("hatchcycle: "+poke[i].hatchcycle);
//				poke[i].basehappy = stat[20];	//Console.WriteLine("basehappy: "+poke[i].basehappy);
//				poke[i].exprate = stat[21];	//Console.WriteLine("exprate: "+poke[i].exprate);
//				poke[i].egggroup1 = stat[22];	//Console.WriteLine("egggroup1: "+poke[i].egggroup1);
//				poke[i].egggroup2 = stat[23];	//Console.WriteLine("egggroup2: "+poke[i].egggroup2);
//				poke[i].ability1 = stat[24];	//Console.WriteLine("ability1: "+poke[i].ability1);
//				poke[i].ability2 = stat[25];	//Console.WriteLine("ability2: "+poke[i].ability2);
//				poke[i].ability3 = stat[26];	//Console.WriteLine("ability3: "+poke[i].ability3);
//				poke[i].flee = stat[27];		//Console.WriteLine("flee: "+poke[i].flee);
//				poke[i].formid = Combine16(stat[29],stat[28]);	//Console.WriteLine("formid: "+poke[i].formid);
//				poke[i].form = Combine16(stat[31],stat[30]);	//Console.WriteLine("form: "+poke[i].form);
//				poke[i].numforms = stat[32];	//Console.WriteLine("numforms: "+poke[i].numforms);
//				poke[i].color = stat[33];		//Console.WriteLine("color: "+poke[i].color);
//				poke[i].baseexp = Combine16(stat[35],stat[34]);	//Console.WriteLine("basexp: "+poke[i].basexp);
//				poke[i].height = Combine16(stat[37],stat[36]);	//Console.WriteLine("height: "+poke[i].height);
//				poke[i].weight = Combine16(stat[39],stat[38]);	//Console.WriteLine("weight: "+poke[i].weight);
//				
//				file.Close();
//				//Console.WriteLine("______________");
//			}
//            return poke;
		}		
		
				
		private sPoke[] readPoke1(int g,int lang)
		{			
			sPoke[] poke = new sPoke[493];
			byte[] game = new byte[poke.Length];
			uint j=19605568;
			
			if(g==2)				//HeartGold
				{
					//a/0/0/2
					if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {	//deutsch oder französisch oder spanisch
						game = ROM.ExtractFile(131);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x45 || lang==0x4B) {			//englisch oder koreanisch
						game = ROM.ExtractFile(131);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x4A) {			//japanisch
						game = ROM.ExtractFile(130);
						j = ROM.FileOffsets[326];
					}
					Console.WriteLine(1);
           		
           			uint firstData = 0x1040;
					j = firstData;
				}
				else if(g==1)	//platinum
				{
					//poketool/personal/personal
					//j = 4160;
					//poketool/pokegra/pokegra
					if(lang==0x44 || lang==0x46 || lang==0x53) {	//deutsch oder französisch oder spanisch
						game = ROM.ExtractFile(394);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x45) {			//englisch
						game = ROM.ExtractFile(419);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x4A) {			//japanisch
						game = ROM.ExtractFile(425);
						j = ROM.FileOffsets[326];
					}
           		
           			uint firstData = 0x1008;
					j = firstData;
				}
				else if(g==0)	//diamond
				{
					//poketool/pokegra/pokegra
					if(lang==0x44 || lang==0x46 || lang==0x53) {	//deutsch oder französisch oder spanisch
						game = ROM.ExtractFile(306);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x45) {			//englisch
						game = ROM.ExtractFile(326);
						j = ROM.FileOffsets[326];
					}
					else if(lang==0x4A) {			//japanisch
						game = ROM.ExtractFile(327);
						j = ROM.FileOffsets[326];
					}
           		
           			uint firstData = 0x1008;
					j = firstData;
				}
				
			
			for(int i=0;i<poke.Length;i++)
			{
				poke[i].basehp = game[j];j++;							//Console.WriteLine("basehp: "+poke[i].basehp);
				poke[i].baseatk = game[j];j++;							//Console.WriteLine("baseatk: "+poke[i].baseatk);
				poke[i].basedef = game[j];j++;							//Console.WriteLine("basedef: "+poke[i].basedef);
				poke[i].basespeed = game[j];j++;						//Console.WriteLine("basespeed: "+poke[i].basespeed);
				poke[i].basespatk = game[j];j++;						//Console.WriteLine("basespatk: "+poke[i].basespatk);
				poke[i].basespdef = game[j];j++;						//Console.WriteLine("basespdef: "+poke[i].basespdef);
				poke[i].type1 = game[j];j++;							//Console.WriteLine("type1: "+poke[i].type1);
				poke[i].type2 = game[j];j++;							//Console.WriteLine("type2: "+poke[i].type2);
				poke[i].catchrate = game[j];j++;						//Console.WriteLine("catchrate: "+poke[i].catchrate);
				poke[i].baseexp = game[j];j++;							//Console.WriteLine("baseexp: "+poke[i].baseexp);
				poke[i].evs = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("evs: "+poke[i].evs);
				poke[i].item1 = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("item1: "+poke[i].item1);
				poke[i].item2 = Combine16(game[j+1],game[j]);j+=2;		//Console.WriteLine("item2: "+poke[i].item2);
				poke[i].gender = game[j];j++;							//Console.WriteLine("gender: "+poke[i].gender);
				poke[i].hatchcycle = game[j];j++;						//Console.WriteLine("hatchcycle: "+poke[i].hatchcycle);
				poke[i].basehappy = game[j];j++;						//Console.WriteLine("basehappy: "+poke[i].basehappy);
				poke[i].exprate = game[j];j++;							//Console.WriteLine("exprate: "+poke[i].exprate);
				poke[i].egggroup1 = game[j];j++;						//Console.WriteLine("egggroup1: "+poke[i].egggroup1);
				poke[i].egggroup2 = game[j];j++;						//Console.WriteLine("egggroup2: "+poke[i].egggroup2);
				poke[i].ability1 = game[j];j++;							//Console.WriteLine("ability1: "+poke[i].ability1);
				poke[i].ability2 = game[j];j++;							//Console.WriteLine("ability2: "+poke[i].ability2);
				poke[i].flee = game[j];j++;								//Console.WriteLine("flee: "+poke[i].flee);
				poke[i].color = game[j];j++;							//Console.WriteLine("color: "+poke[i].color);

				j+=18;
			}
			return poke;
		}
		
		public static Color[] newPaletteFamily(int natInd,sPoke[] poke,Color[] palette,int mode, int g,Random rnd,float[] hu,float[] sa,float[] be,bool colorSelected,Color selColor)
		{
					
			string[] lines;
	        if(g==0 || g==1)
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
	        }
	        else
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
	        }
			char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

			string[] words = lines[natInd].Split(delimiterChars);
			
			int chanceC = rnd.Next(0,4);
			int chance = rnd.Next(0,1);
			float mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			
			for(int s =1;s<words.Length;s++)
				        {
				if((poke[natInd-1].color==7 || poke[natInd-1].color==8 || poke[natInd-1].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
 							int colInd = Int32.Parse(words[s]);
 							float hue = palette[s].GetHue();
 							float sat = palette[s].GetSaturation();
 							float bri = palette[s].GetBrightness();
 							
 							
 								
 								
 								if(chance==0)
 								{
 									hue = (palette[s].GetHue()+hu[colInd])%360f;
 									if(colorSelected==true && chanceC==0 && colInd==mainColor)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								else
 								{
 									float diff = 360f-hu[colInd];
 									hue = (palette[s].GetHue()+diff)%360f;
 									if(colorSelected==true && chanceC==0 && colInd==mainColor)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								if(mode==5 || mode==7 || mode==8)
 								{
 									if(colorSelected==true && chanceC==0 && colInd==mainColor)
 									{
 										sat = (palette[s].GetSaturation()*2f+selColor.GetSaturation()*3f)/5f;
 										bri = (palette[s].GetBrightness()*2f+selColor.GetBrightness()*3f)/5f;
 										
 									}
 									else
 									{
 										sat = (palette[s].GetSaturation()*3f+sa[colInd]*2f)/5f;
 										bri = (palette[s].GetBrightness()*3f+be[colInd]*2f)/5f;
 									}
 								}
 								else	// mode==6
 								{
 									sat = palette[s].GetSaturation();
 									bri = palette[s].GetBrightness();
 								}
 								
 								if(sat>0.9f)
 									sat = sat*0.9f;
// 								if(bri>0.9f)
// 									bri = bri*0.9f;
 								
 							 							
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	//System.Console.WriteLine("Hue: {0} Sat: {1} Bri:{2}",hu[colorInd],palette[0][s].GetSaturation(),palette[0][s].GetBrightness());
					            	palette[s] = FromAhsb(255,hue,sat,bri);
									//checksum++;
									//check[s] = true;
					           			        								
				        }
			}
 						return palette;
		}
		
		
		public int ReadBytesFamily(Random rnd,sPoke[] poke, int mode,int g,string file,bool colorSelected,Color selColor)
		{
			//int j = 110422356; //first data bit of Bulbasaur //60 Bytes until Bisaknosp
								//40 Bytes in use
		
				//Offset a/0/0/4 = 59650560 
				//Offset Bulbasaur NCLR = 059650560 + 123572 = 59774132
					//Offset palette = Offset Bulbasaur NCLR + 40 Byte = 59774172
					//Length palette = 32 Byte
				//Length of one NCLR: 72 bytes 
				
				byte[] game;
				if(g==0) 	//diamond
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(337);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(317);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(338);
					}
				}
				else if(g==1)	//platinum
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(434);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(409);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(440);
					}
				}
				else if(g==2)	//heartgold
				{
					if(lang==0x45 || lang==0x4B) {
						 game = ROM.ExtractFile(133);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(133);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(132);
					}
				}
				else if(g==3)	//black
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(246);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(246);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(246);
					}
				}
				else //if(g==4)	//black2
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(351);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(351);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(351);
					}
				}
				int offsetNarc = 0;
				int l=1,i;
				int last;
//				if(g==4)
//				{
//					int numOfNarc = 0;
//					for(int k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<6)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					
//					int firstData = 0;
//					int counter = 0;
//					for(int k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					
//					l=1;
//					//i=100866560+129812;
//					i = firstData;
//					last = 650;
//				}
//				else if(g==3)
//				{
//					int numOfNarc = 0;
//					for(int k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<7)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					
//					int firstData = 0;
//					int counter = 0;
//					for(int k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					
//					i = firstData;
//					//i = 59650560+123572;
//					last = 650;
//				}
//				else if(g==2)
//				{
////					l=1;
////					i = 19707108;
//					int numOfNarc = 0;
//					for(int k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<26)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					int firstData = 0;
//					int counter = 0;
//					for(int k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
//					last = 494;
//				}
//				else if(g==1)
//				{
////					l=1;
////					if(lang==70)
////					{
////						i = 32351744+75492;
////					}
////					else
////					{
////						i = 32274432+75492;
////					}
//					
//					int numOfNarc = 0;
//					for(int k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<29)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					int firstData = 0;
//					int counter = 0;
//					for(int k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
//					
//					last = 494;
//				}
//				else
//				{
////					l=1;
////					i = 16971776+75492;
//					int numOfNarc = 0;
//					for(int k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<24)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					int firstData = 0;
//					int counter = 0;
//					for(int k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
//					
//					last = 494;
//				}
				
if(g==4)
{
	last = 650;
}
else if(g==3)
{
	last = 650;
}
else if(g==2)
{
	last = 494;
}
else if(g==1)
{
	last = 494;
}
else
{
	last = 494;
}
				
				Color[,] pal = new Color[last,16];
				float[] hu = new float[16];
				float[] sa = new float[16];
				float[] be = new float[16];
				
				int[,] stages = new int[last,16];
				int[,] word = new int[last,16];
				int[] mainColor = new int[last];
				
				string[] fam = System.IO.File.ReadAllLines(".\\families.txt");
				string[,] families = new string[fam.Length,8];
				int[] stage = new int[8];
				
				
				
								
				for(l=0;l<fam.Length;l++)
				{
					string [] fami = fam[l].Split(new Char [] {' ', ',', '.', ':', '\t' });
					int k = 0;
			        for(int j=0;j<fami.Length;j++)
			        {
			        	if (fami[j].Trim() != "")
						{
			        		families[l,k] = fami[j];
			        		k++;
			        	}
					}
			        
			        
			        if(mode==5 || mode==8)
			{
				for(i=0;i<hu.Length;i++)
				{
					hu[i] = (float)(rnd.Next(0, 361));
					sa[i] = (float)(rnd.NextDouble());
					be[i] = (float)(rnd.NextDouble());
				}
			}
			if(mode==6)
			{
				for(i=0;i<hu.Length;i++)
				{						
					hu[i] = (float)(rnd.Next(0, 361));
					sa[i] = (float)0f;
					be[i] = (float)0f;
				}
			}
			else if(mode==7)
			{
				for(i=0;i<hu.Length;i++)
				{
					hu[i] = (float)(rnd.Next(0, 361)/10f);
					sa[i] = (float)(rnd.NextDouble());
					be[i] = (float)(rnd.NextDouble());
				}
			}
			      
			        string[] lines;
			      			        
			        if(g==0 || g==1)
			        {
			        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
			        }
			        else
			        {
			        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
			        }
					char[] delimiterChars = { ' ', ',', '.', ':', '\t' };	
		
					int r = rnd.Next(1,16);
					int temp = 0;
					int st=last;
			        for(int j=0;j<k;j++)
			        {			   
			        	stage[j] = Convert.ToInt32(families[l,j]);
			        	stages[l,j] = stage[j];
			        	st = stage[j];
			        	if(stages[l,j]>=last)
			        	{
			        		break;
			        	}
			        	string[] words = lines[stage[j]].Split(delimiterChars);
			        	for(int p=0;p<words.Length;p++)
			        	{
			        		word[l,p] = Int32.Parse(words[p]);
			        	}
			        	
			        
			        	if(j==0)
			        	{
			        		temp = word[l,r];
			        	}
			        	
			        	mainColor[stages[l,j]] = temp;
						Console.WriteLine("stage: {0} maincolor: {1}",stages[l,j],mainColor[stages[l,j]]);
			        
			        	
			        	for(i=0;i<hu.Length;i++)
			        	{
			        		pal[stage[j],i] = FromAhsb(255,hu[i],sa[i],be[i]);
			        	}
			        }
			        if(st>=last)
			        	{
			        		break;
			        	}
				}
			
				Color[] palette = new Color[16];
				
//				l=0;
//				i = 59650560;
//				
//				if(g==4)
//				{
//					l=1;
//					i=100866560+129812;
//					last = 650;
//				}
//				else if(g==3)
//				{
//					i = 59650560;
//					last = 650;
//				}
//				else if(g==2)
//				{
//					l=1;
//					i = 19707108;
//					last = 494;
//				}
//				else if(g==1)
//				{
//					l=1;
//					if(lang==70)
//					{
//						i = 32351744+75492;
//					}
//					else
//					{
//					i = 32274432+75492;
//					}
//					last = 494;
//				}
//				else
//				{
//					l=1;
//					i = 16971776+75492;
//					last = 494;
//				}
				if(g==4)
				{
					int numOfNarc = 0;
					for(int k=0;k<game.Length-10;k++)
					{
						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
						{
							if(numOfNarc<6)
							{
								numOfNarc = numOfNarc+1;
							}
							else
							{
								offsetNarc = k;
								break;
							}
						}
					}
					
					
					int firstData = 0;
					int counter = 0;
					for(int k=offsetNarc;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					
					l=1;
					//i=100866560+129812;
					i = firstData;
					last = 650;
				}
				else if(g==3)
				{
					int numOfNarc = 0;
					for(int k=0;k<game.Length-10;k++)
					{
						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
						{
							if(numOfNarc<7)
							{
								numOfNarc = numOfNarc+1;
							}
							else
							{
								offsetNarc = k;
								break;
							}
						}
					}
					
					
					int firstData = 0;
					int counter = 0;
					for(int k=offsetNarc;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					
					i = firstData;
					//i = 59650560+123572;
					last = 650;
				}
				else if(g==2)
				{
//					l=1;
//					i = 19707108;
					int numOfNarc = 0;
					for(int k=0;k<game.Length-10;k++)
					{
						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
						{
							if(numOfNarc<26)
							{
								numOfNarc = numOfNarc+1;
							}
							else
							{
								offsetNarc = k;
								break;
							}
						}
					}
					
					int firstData = 0;
					int counter = 0;
					for(int k=offsetNarc;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					i = firstData;
					last = 494;
				}
				else if(g==1)
				{
//					l=1;
//					if(lang==70)
//					{
//						i = 32351744+75492;
//					}
//					else
//					{
//						i = 32274432+75492;
//					}
					
					int numOfNarc = 0;
					for(int k=0;k<game.Length-10;k++)
					{
						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
						{
							if(numOfNarc<29)
							{
								numOfNarc = numOfNarc+1;
							}
							else
							{
								offsetNarc = k;
								break;
							}
						}
					}
					
					int firstData = 0;
					int counter = 0;
					for(int k=offsetNarc;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					i = firstData;
					
					last = 494;
				}
				else
				{
//					l=1;
//					i = 16971776+75492;
					int numOfNarc = 0;
					for(int k=0;k<game.Length-10;k++)
					{
						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
						{
							if(numOfNarc<24)
							{
								numOfNarc = numOfNarc+1;
							}
							else
							{
								offsetNarc = k;
								break;
							}
						}
					}
					
					int firstData = 0;
					int counter = 0;
					for(int k=0x123E4;counter<=2;k++)
					{
						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
						{
							counter++;
							if(counter==3)
							{
								firstData = k;
							}
						}
					}
					i = firstData;
					
					if(g<3)
					{
						i = 0x126E4;
						last = 492;
					}
					else if(g==3)
					{
						i = 0x1E2B4;
						last = 649;
					}
					else
					{
						i = 0x1FB14;
						last = 649;
					}
				}
				byte[] bytearray = new byte[32];				
				int offset;
				l=1;
				
				
				for(;l<last;i++)
				{
					if(game[i]==0x52 && game[i+1]==0x4C && game[i+2]==0x43 && game[i+3]==0x4E)
					{
						offset = i+40;
						
						for(int p=offset;p<offset+32;p++)
						{
							bytearray[p-offset] = game[p];
						}
						palette = Actions.BGR555ToColor(bytearray);
						
						for(int j=0;j<palette.Length;j++)
						{
							hu[j] = pal[l,j].GetHue();
							sa[j] = pal[l,j].GetSaturation();
							be[j] = pal[l,j].GetBrightness();
						}
						
						
						
						if(mode==8)
						{
							palette = newPaletteOne(l,poke,palette,mode,g,rnd,mainColor[l],hu,sa,be,colorSelected,selColor);
						}
						else{
							palette = newPaletteFamily(l,poke,palette,mode,g,rnd,hu,sa,be,colorSelected,selColor);
						}
														
						int chance = rnd.Next(0,5);
//						for(int m=0;m<palette.Length;m++)
//						{
//							if(chance==0)
//							{
//								palette[m] = FromAhsb(255,(palette[m].GetHue()+selColor.GetHue()*2)/3f,(palette[m].GetSaturation()*2+selColor.GetSaturation())/3f,(palette[m].GetBrightness()*2+selColor.GetBrightness())/3f);
//							}
//						}
													
						bytearray = Actions.ColorToBGR555(palette);
						
						for(int p=offset;p<offset+32;p++)
						{
							game[p] = bytearray[p-offset];
						}
						
											
						i = i+75;
						l++;
					}
										
				}

				/*if(g==1)	//platinum
				{
					l=0;
					i= 44056576+75492;	//platinum pokegra
					
					for(;l<last;i++)
					{
						if(game[i]==0x52 && game[i+1]==0x4C && game[i+2]==0x43 && game[i+3]==0x4E)
						{
							offset = i+40;
							
							for(int p=offset;p<offset+32;p++)
							{
								bytearray[p-offset] = game[p];
							}
								
							for(int j=0;j<palette.Length;j++)
							{
								palette[j] = pal[l,j];
							}			
							
																		
							bytearray = Actions.ColorToBGR555(palette);
							
							for(int p=offset;p<offset+32;p++)
							{
								game[p] = bytearray[p-offset];
							}
							
												
							i = i+75;
							l++;
						}
											
					}

				}*/
				
				if(g==0)
				{
					if(lang==0x45) {
						ROM.ReplaceFile(337,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(317,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(338,game);
					}
				}
				else if(g==1) 
				{
					if(lang==0x45) {
						ROM.ReplaceFile(434,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(409,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(440,game);
					}
				}
				else if(g==2) 
				{
					if(lang==0x45 || lang==0x4B) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(132,game);
					}
				}
				else if(g==3) 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(246,game);
					}
				}
				else if(g==4) 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(351,game);
					}
				}
				
//				Console.WriteLine("Writing...");
//				int position = file.LastIndexOf('.');
//				string f = file.Substring(0,position);
//				string path = f+"Random.nds";
//				Console.WriteLine(path);
//				File.WriteAllBytes(path,game);
				
//				if(g==4)
//				{
//					File.WriteAllBytes("Black2Random.nds",game);
//				}
//				else if(g==3)
//				{
//					File.WriteAllBytes("BlackRandom.nds",game);
//				}
//				else if(g==2)
//				{
//					File.WriteAllBytes("HeartGoldRandom.nds",game);
//				}
//				else if(g==1)
//				{
//					File.WriteAllBytes("PlatinumRandom.nds",game);
//				}
//				else if(g==0)
//				{
//					File.WriteAllBytes("DiamondRandom.nds",game);
//				}
				
//				palette = Actions.BGR555ToColor(pal);
//				
//				for(i=0;i<palette.Length;i++)
//				{
//					Console.WriteLine("R: {0} G: {1} B: {2}",palette[i].R,palette[i].G,palette[i].B);
//				}
				
			return 0;
		}
		
		
		public int ReadBytes(Random rnd,sPoke[] poke, int mode,int g,string file,int lang,bool colorSelected,Color selColor)
		{
//			byte[] game = File.ReadAllBytes("Black2.nds");; 
//			
//			if(g==4)
//			{
//				game = File.ReadAllBytes("Black2.nds");
//			}
//			else if(g==3)
//			{
//				game = File.ReadAllBytes("Black.nds");
//			}
//			else if(g==2)
//			{
//				game = File.ReadAllBytes("HeartGold.nds");
//			}
//			else if(g==1)
//			{
//				game = File.ReadAllBytes("Platinum.nds");
//			}
//			else if(g==0)
//			{
//				game = File.ReadAllBytes("Diamond.nds");
//			}
			//int j = 110422356; //first data bit of Bulbasaur //60 Bytes until Bisaknosp
								//40 Bytes in use
		
				//Offset a/0/0/4 = 59650560 
				//Offset Bulbasaur NCLR = 059650560 + 123572 = 59774132
					//Offset palette = Offset Bulbasaur NCLR + 40 Byte = 59774172
					//Length palette = 32 Byte
				//Length of one NCLR: 72 bytes 
			
				
				byte[] game;
				if(g==0) 	//diamond
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(337);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(317);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(338);
					}
				}
				else if(g==1)	//platinum
				{
					if(lang==0x45) {
						 game = ROM.ExtractFile(434);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						game = ROM.ExtractFile(409);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(440);
					}
				}
				else if(g==2)	//heartgold
				{
					if(lang==0x45 || lang==0x4B) {
						 game = ROM.ExtractFile(133);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(133);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(132);
					}
				}
				else if(g==3)	//black
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(246);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(246);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(246);
					}
				}
				else //if(g==4)	//black2
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						 game = ROM.ExtractFile(351);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						game = ROM.ExtractFile(351);
					}
					else {//if(lang==0x4A) {
						game = ROM.ExtractFile(351);
					}
				}
				int l=1;
				uint i;
				int last;
//				if(g==4)
//				{
//					l=1;
//					i=100866560+129812;
//					last = 650;
//				}
//				else if(g==3)
//				{
//					i = 59650560;
//					last = 650;
//				}
//				else if(g==2)
//				{
//					l=1;
//					i = 19707108;
//					last = 494;
//				}
//				else if(g==1)
//				{
//					l=1;
//					i = 32274432+75492;
//					last = 494;
//				}
//				else
//				{
//					l=1;
//					i = 16971776+75492;
//					last = 494;
//				}
				
				//uint offsetNarc = 0;
				
				if(g==4)
				{
//					int numOfNarc = 0;
//					for(uint k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<6)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					uint firstData = 0;
//					int counter = 0;
//					for(uint k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					
//					l=1;
////					if(random==false)
////					{
////						i=100866560+129812;
////					}
////					else
////					{
////						i=100859392+121016;
////					}
//					
//					//position von file2 finden
//					
//					
//					i = firstData;
//					Console.WriteLine("offset: "+offsetNarc);
//					last = 650;
				}
				else if(g==3)
				{
//					int numOfNarc = 0;
//					for(uint k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<7)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					
//					uint firstData = 0;
//					int counter = 0;
//					for(uint k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					
//
//						i = firstData;
//						Console.WriteLine("number of Narcs: "+numOfNarc+"	offset: "+offsetNarc);
//					
//					last = 650;
				}
				else if(g==2)
				{
//					int numOfNarc = 0;
//					for(uint k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<26)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					uint firstData = 0;
//					int counter = 0;
//					for(uint k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
//					
//					last = 494;
				}
				else if(g==1)
				{
//					
//					int numOfNarc = 0;
//					for(uint k=0;k<game.Length-10;k++)
//					{
//						if(game[k]==0x4E && game[k+1]==0x41 && game[k+2]==0x52 && game[k+3]==0x43 && game[k+4]==0xFE && game[k+5]==0xFF)
//						{
//							if(numOfNarc<29)
//							{
//								numOfNarc = numOfNarc+1;
//							}
//							else
//							{
//								offsetNarc = k;
//								break;
//							}
//						}
//					}
//					
//					uint firstData = 0;
//					int counter = 0;
//					for(uint k=offsetNarc;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==3)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
//					
					last = 494;
				}
				else
				{
					l=1;
//					if(random==false)
//					{
//						i = 16971776+75492;
//					}
//					else
//					{
//						i=45474304+75492;
//					}
					
				}
					
				
					
//					uint firstData = 0;
//					int counter = 0;
//					for(uint k= 0x126E4;counter<=2;k++)
//					{
//						if(game[k]==0x52 && game[k+1]==0x4C && game[k+2]==0x43 && game[k+3]==0x4E && game[k+4]==0xFF && game[k+5]==0xFE)
//						{
//							counter++;
//							if(counter==2)
//							{
//								firstData = k;
//							}
//						}
//					}
//					i = firstData;
					
				if(g<3)
				{
					i = 0x126E4;
					last = 492;
				}
				else if(g==3)
				{
					i = 0x1E2B4;
					last = 649;
				}
				else
				{
					i = 0x1FB14;
					last = 649;
				}				
				
				
				
				byte[] bytearray = new byte[32];
				uint[] offset = new uint[last];
				Color[,] pal = new Color[last,16];
				for(;l<last;i++)
				{
					if(game[i]==0x52 && game[i+1]==0x4C && game[i+2]==0x43 && game[i+3]==0x4E)
					{
						offset[l] = i+40;
						
						for(uint p=offset[l];p<offset[l]+32;p++)
						{
							bytearray[p-offset[l]] = game[p];
						}
						Color[] palette = Actions.BGR555ToColor(bytearray);
						
						
//						for(int k=0;k<palette.Length;k++)
//						{
//							Console.WriteLine("Palette: {0}	Color: {1}",l,pal[l,k]);
//						}
						
						if(mode==4)
						{
							palette = newPaletteOne(l,poke,palette,mode,g,rnd,colorSelected,selColor);
						}
						else
						{
							palette = newPalette(l,poke,palette,mode,g,rnd,colorSelected,selColor);
						}
						
						int chance = rnd.Next(0,5);
						for(int m=0;m<palette.Length;m++)
						{
//							if(chance==0)
//							{
//								palette[m] = FromAhsb(255,(palette[m].GetHue()+selColor.GetHue()*2)/3f,(palette[m].GetSaturation()*2+selColor.GetSaturation())/3f,(palette[m].GetBrightness()*2+selColor.GetBrightness())/3f);
//							}
							pal[l,m] = palette[m];
						}
						
						bytearray = Actions.ColorToBGR555(palette);
						
						for(uint p=offset[l];p<offset[l]+32;p++)
						{
							game[p] = bytearray[p-offset[l]];
						}
						
											
						i = i+75;						
						l++;
						
						
					}
										
				}
				
				if(g==0)
				{
					if(lang==0x45) {
						ROM.ReplaceFile(337,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(317,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(338,game);
					}
				}
				else if(g==1) 
				{
					if(lang==0x45) {
						ROM.ReplaceFile(434,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53) {
						ROM.ReplaceFile(409,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(440,game);
					}
				}
				else if(g==2) 
				{
					if(lang==0x45 || lang==0x4B) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(133,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(132,game);
					}
				}
				else if(g==3)	//black 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(246,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(246,game);
					}
				}
				else if(g==4)	//black2 
				{
					if(lang==0x45 || lang==0x4B || lang==0x4F) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x44 || lang==0x46 || lang==0x53 || lang==0x49) {
						ROM.ReplaceFile(351,game);
					}
					else if(lang==0x4A) {
						ROM.ReplaceFile(351,game);
					}
				}
				
//				Console.WriteLine("Writing...");
//				int position = file.LastIndexOf('.');
//				string f = file.Substring(0,position);
//				string path = f+"Random.nds";
//				Console.WriteLine(path);
//				File.WriteAllBytes(path,game);
				
			return 0;
		}
		
		public static Color[] newPalette(int natInd,sPoke[] poke,Color[] palette,int mode, int g, Random rnd,bool colorSelected,Color selColor)
		{
			float[] hu = new float[palette.Length];
			float[] sa = new float[palette.Length];
			float[] be = new float[palette.Length];
			
			Console.WriteLine("natInd: {0}	type1: {1}	type2:{2}",natInd,poke[natInd].type1,poke[natInd].type2);
			
			
			if(mode==1 || mode==4)
			{
				for(int i=0;i<palette.Length;i++)
				{
					hu[i] = (float)(rnd.Next(0, 361));
					sa[i] = (float)(rnd.NextDouble());
					be[i] = (float)(rnd.NextDouble());
				}
			}
			if(mode==2)
			{
				for(int i=0;i<palette.Length;i++)
				{						
					hu[i] = (float)(rnd.Next(0, 361));
					sa[i] = (float)0f;
					be[i] = (float)0f;
				}
			}
			else if(mode==3)
			{
				for(int i=0;i<palette.Length;i++)
				{
					hu[i] = (float)(rnd.Next(0, 361)/10f);
					sa[i] = (float)(rnd.NextDouble());
					be[i] = (float)(rnd.NextDouble());
				}
			}
			
			
			string[] lines;
	        if(g==0 || g==1)
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
	        }
	        else
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
	        }
			char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

			string[] words = lines[natInd].Split(delimiterChars);
			
			for(int i=0;i<words.Length;i++)
			{
				Console.Write(words[i]+" ");
			}
			Console.WriteLine(" ");
			
			int mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			int chanceC = rnd.Next(0,4);
			int chance = rnd.Next(0,2);
			for(int s =1;s<words.Length;s++)
				        {
				if((poke[natInd-1].color==7 || poke[natInd-1].color==8 || poke[natInd-1].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
 							int colInd = Int32.Parse(words[s]);
 							float hue = palette[s].GetHue();
 							float sat = palette[s].GetSaturation();
 							float bri = palette[s].GetBrightness();
 							
 								
 								if(chance==0)
 								{
 									hue = (palette[s].GetHue()+hu[colInd])%360f;
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								else
 								{
 									float diff = 360f-hu[colInd];
 									hue = (palette[s].GetHue()+diff)%360f;
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								if(mode==1 || mode==3 || mode==4)
 								{
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										sat = (palette[s].GetSaturation()*2f+selColor.GetSaturation()*3f)/5f;
 										bri = (palette[s].GetBrightness()*2f+selColor.GetBrightness()*3f)/5f;
 										
 									}
 									else
 									{
 										sat = (palette[s].GetSaturation()*3f+sa[colInd]*2f)/5f;
 										bri = (palette[s].GetBrightness()*3f+be[colInd]*2f)/5f;
 									}
 								}
 								else	//mode==2
 								{
 									sat = palette[s].GetSaturation();
 									bri = palette[s].GetBrightness();
 								}
 								
 								if(sat>0.9f)
 									sat = sat*0.9f;
// 								if(bri>0.9f)
// 									bri = bri*0.9f;
 							 							
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	//System.Console.WriteLine("Hue: {0} Sat: {1} Bri:{2}",hu[colorInd],palette[0][s].GetSaturation(),palette[0][s].GetBrightness());
					            	palette[s] = FromAhsb(255,hue,sat,bri);
									//checksum++;
									//check[s] = true;
					           			        								
				        }
			}
 						return palette;
		}
		
		public static Color FromAhsb(int alpha, float hue, float saturation, float brightness)
		{
		
		    if (0 > alpha || 255 < alpha)
		    {
		        throw new ArgumentOutOfRangeException("alpha", alpha,
		          "Value must be within a range of 0 - 255.");
		    }
		    if (0f > hue || 360f < hue)
		    {
		        throw new ArgumentOutOfRangeException("hue", hue,
		          "Value must be within a range of 0 - 360.");
		    }
		    if (0f > saturation || 1f < saturation)
		    {
		        throw new ArgumentOutOfRangeException("saturation", saturation,
		          "Value must be within a range of 0 - 1.");
		    }
		    if (0f > brightness || 1f < brightness)
		    {
		        throw new ArgumentOutOfRangeException("brightness", brightness,
		          "Value must be within a range of 0 - 1.");
		    }
		
		    if (0 == saturation)
		    {
		        return Color.FromArgb(alpha, Convert.ToInt32(brightness * 255),
		          Convert.ToInt32(brightness * 255), Convert.ToInt32(brightness * 255));
		    }
		
		    float fMax, fMid, fMin;
		    int iSextant, iMax, iMid, iMin;
		
		    if (0.5 < brightness)
		    {
		        fMax = brightness - (brightness * saturation) + saturation;
		        fMin = brightness + (brightness * saturation) - saturation;
		    }
		    else
		    {
		        fMax = brightness + (brightness * saturation);
		        fMin = brightness - (brightness * saturation);
		    }
		
		    iSextant = (int)Math.Floor(hue / 60f);
		    if (300f <= hue)
		    {
		        hue -= 360f;
		    }
		    hue /= 60f;
		    hue -= 2f * (float)Math.Floor(((iSextant + 1f) % 6f) / 2f);
		    if (0 == iSextant % 2)
		    {
		        fMid = hue * (fMax - fMin) + fMin;
		    }
		    else
		    {
		        fMid = fMin - hue * (fMax - fMin);
		    }
		
		    iMax = Convert.ToInt32(fMax * 255);
		    iMid = Convert.ToInt32(fMid * 255);
		    iMin = Convert.ToInt32(fMin * 255);
		
		    switch (iSextant)
		    {
		        case 1:
		            return Color.FromArgb(alpha, iMid, iMax, iMin);
		        case 2:
		            return Color.FromArgb(alpha, iMin, iMax, iMid);
		        case 3:
		            return Color.FromArgb(alpha, iMin, iMid, iMax);
		        case 4:
		            return Color.FromArgb(alpha, iMid, iMin, iMax);
		        case 5:
		            return Color.FromArgb(alpha, iMax, iMin, iMid);
		        default:
		            return Color.FromArgb(alpha, iMax, iMid, iMin);
		    }
	
		}
		
		
		public static Color[] newPaletteOne(int natInd,sPoke[] poke,Color[] palette,int mode, int g,Random rnd,bool colorSelected,Color selColor)
		{
			float[] hu = new float[palette.Length];
			float[] sa = new float[palette.Length];
			float[] be = new float[palette.Length];
			
			//Console.WriteLine("natInd: {0}	type1: {1}	type2:{2}",natInd,poke[natInd].type1,poke[natInd].type2);
			
			
			
				for(int i=0;i<palette.Length;i++)
				{
					hu[i] = (float)(rnd.Next(0, 361));
					sa[i] = (float)(rnd.NextDouble());
					be[i] = (float)(rnd.NextDouble());
				}
			
			
			
			string[] lines;
	        if(g==0 || g==1)
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
	        }
	        else
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
	        }
			char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

			string[] words = lines[natInd].Split(delimiterChars);
			
			int mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			int chanceC = rnd.Next(0,4);
			int chance = rnd.Next(0,2);
			for(int s =1;s<words.Length;s++)
				        {
				if((poke[natInd-1].color==7 || poke[natInd-1].color==8 || poke[natInd-1].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
 							int colInd = Int32.Parse(words[s]);
 							float hue = palette[s].GetHue();
 							float sat = palette[s].GetSaturation();
 							float bri = palette[s].GetBrightness();
 							
 							if(mainColor==colInd)
 							{
 								if(chance==0)
 								{
 									hue = (palette[s].GetHue()+hu[colInd])%360f;
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								else
 								{
 									float diff = 360f-hu[colInd];
 									hue = (palette[s].GetHue()+diff)%360f;
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										sat = (palette[s].GetSaturation()*2f+selColor.GetSaturation()*3f)/5f;
 										bri = (palette[s].GetBrightness()*2f+selColor.GetBrightness()*3f)/5f;
 										
 									}
 									else
 									{
 										sat = (palette[s].GetSaturation()*3f+sa[colInd]*2f)/5f;
 										bri = (palette[s].GetBrightness()*3f+be[colInd]*2f)/5f;
 									}
 								
 								
 								
 							}
 							if(sat>0.9f)
 									sat = sat*0.9f;
// 								if(bri>0.9f)
// 									bri = bri*0.9f;
 							 							
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	//System.Console.WriteLine("Hue: {0} Sat: {1} Bri:{2}",hu[colorInd],palette[0][s].GetSaturation(),palette[0][s].GetBrightness());
					            	palette[s] = FromAhsb(255,hue,sat,bri);
									//checksum++;
									//check[s] = true;
					           			        								
				        }
			}
 						return palette;
		}
		
		public static Color[] newPaletteOne(int natInd,sPoke[] poke,Color[] palette,int mode, int g,Random rnd,int mainColor,float[] hu,float[] sa,float[] be,bool colorSelected,Color selColor)
		{
			
			
			//Console.WriteLine("natInd: {0}	type1: {1}	type2:{2}",natInd,poke[natInd].type1,poke[natInd].type2);
			
			
			
				
			
			
			
			string[] lines;
	        if(g==0 || g==1)
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes_gen4.txt");
	        }
	        else
	        {
	        	lines = System.IO.File.ReadAllLines(@".\Palettes.txt");
	        }
			char[] delimiterChars = { ' ', ',', '.', ':', '\t' };

			string[] words = lines[natInd].Split(delimiterChars);
			
			bool colorContained = false;
			for(int i=1;i<words.Length;i++)
			{
				int temp = Int32.Parse(words[i]);
				if(temp==mainColor)
				{
					colorContained=true;
				}
			}
			
			//Console.WriteLine("maincolor1: "+mainColor+" colorContained: "+colorContained);
			if(colorContained==false)
			{
				mainColor = Int32.Parse(words[rnd.Next(1,16)]);
			}
			//Console.WriteLine("maincolor2: "+mainColor);
			
			
			//int mainColor;// = Int32.Parse(words[colorPosition]); //find mainColor in readbytesfamily for the whole family
			int chanceC = rnd.Next(0,4); 
			int chance = rnd.Next(0,1);
			for(int s =1;s<words.Length;s++)
				        {
				if((poke[natInd-1].color==7 || poke[natInd-1].color==8 || poke[natInd-1].color==4) || (Int32.Parse(words[s])!=4 && Int32.Parse(words[s])!=7 && Int32.Parse(words[s])!=8))
				{
 							int colInd = Int32.Parse(words[s]);
 							float hue = palette[s].GetHue();
 							float sat = palette[s].GetSaturation();
 							float bri = palette[s].GetBrightness();
 							
 							if(mainColor==colInd)
 							{
 								if(chance==0)
 								{
 									hue = hu[1];
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											float diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											float diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								else
 								{
 									float diff = 360f-hu[1];
 									hue = (palette[s].GetHue()+diff)%360f;
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										if(selColor.GetHue()<palette[s].GetHue())
 										{
 											diff = 360f-palette[s].GetHue()+selColor.GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 										else
 										{
 											diff = selColor.GetHue()-palette[s].GetHue();
 											hue = (palette[s].GetHue()+diff)%360f;
 										}
 									}
 								}
 								
 									if(colorSelected==true && chanceC==0 && mainColor==colInd)
 									{
 										sat = (palette[s].GetSaturation()*2f+selColor.GetSaturation()*3f)/5f;
 										bri = (palette[s].GetBrightness()*2f+selColor.GetBrightness()*3f)/5f;
 										
 									}
 									else
 									{
 										sat = (palette[s].GetSaturation()*3f+sa[0]*2f)/5f;
 										bri = (palette[s].GetBrightness()*3f+be[0]*2f)/5f;
 									}
 								
 								
 								
 							}
 							
 							 	if(sat>0.9f)
 									sat = sat*0.9f;
// 								if(bri>0.9f)
// 									bri = bri*0.9f;	
					            	//float hue = (palette[0][s].GetHue()+hu[colorInd])%360f;
					            	//System.Console.WriteLine("Hue: {0} Sat: {1} Bri:{2}",hu[colorInd],palette[0][s].GetSaturation(),palette[0][s].GetBrightness());
					            	palette[s] = FromAhsb(255,hue,sat,bri);
									//checksum++;
									//check[s] = true;
					           			        								
				        }
			}
 						return palette;
		}
		
		public int ChangeTrainer()
		{	
			if(g==3) {
			byte[] TrainerBackSprites = ROM.ExtractFile(315);	//1. palette boy, 2nd palette girl
			int l=0;
			int offsetGirl;
			byte[] bytearrayGirl = new byte[32];
			int offsetBoy;
			byte[] bytearrayBoy = new byte[32];
			
			for(int i=0;l<2;i++)
			{
				if(TrainerBackSprites[i]==0x52 && TrainerBackSprites[i+1]==0x4C && TrainerBackSprites[i+2]==0x43 && TrainerBackSprites[i+3]==0x4E)
				{	
					if(l==1) {
					offsetGirl = i+40;
					
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						bytearrayGirl[p-offsetGirl] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {					
								palette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==1) {
								palette[3] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==2) {
								palette[6] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[6].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[7].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								palette[9] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[9].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[10] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[10].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
							}
							else {
								palette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(palette);
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						TrainerBackSprites[p] = bytearrayGirl[p-offsetGirl];
					}					
										
					i = i+75;
					l++;
					}
					else {	//Boy____________________________________________
						offsetBoy = i+40;
					
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						bytearrayBoy[p-offsetBoy] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==0) {					
								for(int k=15;k<16;k++) {
									//if(palette[k].GetBrightness()>0.20)
										palette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
							}
							else if(j==1) {
								
									//if(palette[k].GetBrightness()>0.20)
									palette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[8] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[8].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==2) {
								
									palette[7] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[7].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								palette[14] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[14].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[10] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[10].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								palette[3] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[3].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[6] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[6].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else {
								for(int k=3;k<5;k++) {
									
										//if(palette[k].GetBrightness()>0.20)
										FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								
									
								}
							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(palette);
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						TrainerBackSprites[p] = bytearrayBoy[p-offsetBoy];
					}					
										
					i = i+75;
					l++;
					}
				}
			}
			
			ROM.ReplaceFile(315,TrainerBackSprites);
			
			//overworld sprites girl
			byte[] TrainerOverworldSprites = ROM.ExtractFile(291);
			int fileOffsetGirl = 0xC370;
			int filePaletteOffsetGirl = fileOffsetGirl+0x4408;
			int fileBikeOffsetGirl = 0x10798;
			int fileBikePaletteOffsetGirl = fileBikeOffsetGirl+ 0x2248;
			int fileSwimOffsetGirl = 0x12A00;
			int fileSwimPaletteOffsetGirl = fileSwimOffsetGirl+0x8f8;
			
			for(int p=filePaletteOffsetGirl;p<filePaletteOffsetGirl+32;p++)
					{
						bytearrayGirl[p-filePaletteOffsetGirl] = TrainerOverworldSprites[p];
					}
					Color[] pallette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {					
								for(int k=5;k<8;k++) {
									pallette[k] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[k].GetBrightness()+Girl[j].GetBrightness())/2f);
								}
							}
							else if(j==1) {
								for(int k=3;k<5;k++) {
									pallette[k] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[k].GetBrightness()+Girl[j].GetBrightness())/2f);
								}
							}
							else if(j==2) {
								for(int k=1;k<3;k++) {
									pallette[k] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[k].GetBrightness()+Girl[j].GetBrightness())/2f);
								}
								pallette[8] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[8].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
//							else if(j==4) {
//								pallette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),pallette[5].GetBrightness());
//							}
							else if(j==3) {
								for(int k=10;k<13;k++) {
									if(k!=11)
										pallette[k] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[k].GetBrightness()+Girl[j].GetBrightness())/2f);
								}
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(pallette);
					
					for(int p=filePaletteOffsetGirl;p<filePaletteOffsetGirl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-filePaletteOffsetGirl];
					}
					for(int p=fileBikePaletteOffsetGirl;p<fileBikePaletteOffsetGirl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-fileBikePaletteOffsetGirl];
					}
					for(int p=fileSwimPaletteOffsetGirl;p<fileSwimPaletteOffsetGirl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-fileSwimPaletteOffsetGirl];
					}
					
					
					//overworld sprite boy
					
			int fileOffsetBoy = 0x53C8;
			int filePaletteOffsetBoy = fileOffsetBoy+0x4408;
			int fileBikeOffsetBoy = 0x97F0;
			int fileBikePaletteOffsetBoy = fileBikeOffsetBoy+ 0x2248;
			int fileSwimOffsetBoy = 0xBA58;
			int fileSwimPaletteOffsetBoy = fileSwimOffsetBoy+0x8F8;
			
			for(int p=filePaletteOffsetBoy;p<filePaletteOffsetBoy+32;p++)
					{
						bytearrayBoy[p-filePaletteOffsetBoy] = TrainerOverworldSprites[p];
					}
					pallette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==0) {					
								for(int k=3;k<5;k++) {
									pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
							}
							else if(j==1) {
								for(int k=1;k<3;k++) {
									pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
							}
							else if(j==2) {
								for(int k=11;k<12;k++) {
									pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
								
							}
							else if(j==3) {
								for(int k=10;k<13;k++) {
									if(k!=11)
										pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
								
							}
							else { //if(j==4) {
								for(int k=5;k<8;k++) {
									pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
								}
							}
//							else {
//								for(int k=10;k<13;k++) {
//									if(k!=11)
//										pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(pallette);
					
					for(int p=filePaletteOffsetBoy;p<filePaletteOffsetBoy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-filePaletteOffsetBoy];
					}
					for(int p=fileBikePaletteOffsetBoy;p<fileBikePaletteOffsetBoy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-fileBikePaletteOffsetBoy];
					}
					for(int p=fileSwimPaletteOffsetBoy;p<fileSwimPaletteOffsetBoy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-fileSwimPaletteOffsetBoy];
					}
					
			ROM.ReplaceFile(291,TrainerOverworldSprites);
			}
			
			else if(g==4) {	//black2
			byte[] TrainerBackSprites = ROM.ExtractFile(419);	//1. palette boy, 2nd palette girl
			int l=0;
			int offsetGirl;
			byte[] bytearrayGirl = new byte[32];
			int offsetBoy;
			byte[] bytearrayBoy = new byte[32];
			
			for(int i=0;l<2;i++)
			{
				if(TrainerBackSprites[i]==0x52 && TrainerBackSprites[i+1]==0x4C && TrainerBackSprites[i+2]==0x43 && TrainerBackSprites[i+3]==0x4E)
				{	
					if(l==1) {
					offsetGirl = i+40;
					
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						bytearrayGirl[p-offsetGirl] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {					
								palette[3] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==1) {
								palette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
										
							}
							else if(j==2) {
								palette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								palette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[10] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[10].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else {
								palette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[6] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[6].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(palette);
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						TrainerBackSprites[p] = bytearrayGirl[p-offsetGirl];
					}					
										
					i = i+75;
					l++;
					}
					else {	//Boy____________________________________________
						offsetBoy = i+40;
					
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						bytearrayBoy[p-offsetBoy] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
								palette[13] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[13].GetBrightness()+Boy[j].GetBrightness())/2f);
							
							}
							else if(j==2) {								
									palette[3] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[3].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								
									palette[15] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[15].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[14] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[14].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[6] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[6].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								palette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else {//if(j==0) {
								palette[7] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[7].GetBrightness()*2f+Boy[j].GetBrightness())/3f);
								palette[8] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[8].GetBrightness()*2f+Boy[j].GetBrightness())/3f);
								palette[9] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[9].GetBrightness()*2f+Boy[j].GetBrightness())/3f);
								palette[10] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[10].GetBrightness()*2f+Boy[j].GetBrightness())/3f);
								
							}
//							else {
//								for(int k=3;k<5;k++) {
//									
//										//if(palette[k].GetBrightness()>0.20)
//										FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								
//									
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(palette);
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						TrainerBackSprites[p] = bytearrayBoy[p-offsetBoy];
					}					
										
					i = i+75;
					l++;
					}
				}
			}
			
			ROM.ReplaceFile(419,TrainerBackSprites);
			
			//overworld sprites girl
			byte[] TrainerOverworldSprites = ROM.ExtractFile(395);
			int file219Girl = 0x14FA94+0x3328;
			int file220Girl = 0x152DDC+ 0x2248;
			int file221Girl = 0x155044+0x8f8;
			int file222Girl = 0x15595c+0x4c0;
			int file223Girl = 0x155E3C+0x4c0;
			int file224Girl = 0x15631c+0xb14;
			int file225Girl = 0x156e50+0x8f8;
			int file226Girl = 0x157768+0x2248;
			int file227Girl = 0x159900+0x61d8;
			
			for(int p=file219Girl;p<file219Girl+32;p++)
					{
						bytearrayGirl[p-file219Girl] = TrainerOverworldSprites[p];
					}
					Color[] pallette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {					
								pallette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[6] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[6].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==1) {
								pallette[3] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==2) {
								pallette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								pallette[8] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[8].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[9] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[9].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else {
								pallette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(pallette);
					
					for(int p=file219Girl;p<file219Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file219Girl];
					}
					for(int p=file220Girl;p<file220Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file220Girl];
					}
					for(int p=file221Girl;p<file221Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file221Girl];
					}
					for(int p=file222Girl;p<file222Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file222Girl];
					}
					for(int p=file223Girl;p<file223Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file223Girl];
					}
					for(int p=file224Girl;p<file224Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file224Girl];
					}
					for(int p=file225Girl;p<file225Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file225Girl];
					}
					for(int p=file226Girl;p<file226Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file226Girl];
					}
					for(int p=file227Girl;p<file227Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file227Girl];
					}
					
					
					
					//overworld sprite boy
					
			int file210Boy = 0x13f960+0x3328;
			int file211Boy = 0x142ca8+ 0x2248;
			int file212Boy = 0x144f10+0x8f8;
			int file213Boy = 0x145828+0x4c0;
			int file214Boy = 0x145d08+0x4c0;
			int file215Boy = 0x1461e8+0xb14;
			int file216Boy = 0x146d1c+0x8f8;
			int file217Boy = 0x147634+0x2248;
			int file218Boy = 0x14989c+0x61d8;
			
			for(int p=file210Boy;p<file210Boy+32;p++)
					{
						bytearrayBoy[p-file210Boy] = TrainerOverworldSprites[p];
					}
					pallette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
								
									pallette[1] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[1].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[3] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[3].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==2) {
								pallette[9] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[9].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[14] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[14].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								pallette[6] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[6].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								
								
							}
							else if(j==4) {
								pallette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[10] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[10].GetBrightness()+Boy[j].GetBrightness())/2f);
								
								
							}
							else { //if(j==4) {
								pallette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[7] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[7].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
//							else {
//								for(int k=10;k<13;k++) {
//									if(k!=11)
//										pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(pallette);
					
					for(int p=file210Boy;p<file210Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file210Boy];
					}
					for(int p=file211Boy;p<file211Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file211Boy];
					}
					for(int p=file212Boy;p<file212Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file212Boy];
					}
					for(int p=file213Boy;p<file213Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file213Boy];
					}
					for(int p=file214Boy;p<file214Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file214Boy];
					}
					for(int p=file215Boy;p<file215Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file215Boy];
					}
					for(int p=file216Boy;p<file216Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file216Boy];
					}
					for(int p=file217Boy;p<file217Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file217Boy];
					}
					for(int p=file218Boy;p<file218Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file218Boy];
					}
					
					
			ROM.ReplaceFile(395,TrainerOverworldSprites);
			}
			
			else if(g==2) {	//heartgold
				byte[] TrainerBackSprites;
				if(g1==21)
					TrainerBackSprites = ROM.ExtractFile(134);	//1. palette boy, 2nd palette girl
				else
					TrainerBackSprites = ROM.ExtractFile(135);
			//int l=-1;
			int offsetGirl;
			byte[] bytearrayGirl = new byte[32];
			int offsetBoy;
			byte[] bytearrayBoy = new byte[32];
			
			
					offsetGirl = 0x7DF38+40;
					
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						bytearrayGirl[p-offsetGirl] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
//							if(j==0) {					
//								palette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
//								palette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
//							
//							}
//							else 
							if(j==1) {
								palette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==2) {
								palette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[10] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[10].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								palette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
							}
							else if(j==4) {
								palette[3] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[7].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(palette);
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						TrainerBackSprites[p] = bytearrayGirl[p-offsetGirl];
					}					
										
					
					//l++;
					
					//Boy____________________________________________
						offsetBoy = 0x75BAC+40;
					
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						bytearrayBoy[p-offsetBoy] = TrainerBackSprites[p];
					}
					palette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
								palette[6] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[6].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[9] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[9].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==2) {
								
									//if(palette[k].GetBrightness()>0.20)
									palette[14] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[14].GetBrightness()+Boy[j].GetBrightness())/2f);
									
							}
							else if(j==3) {
								
									palette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								palette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==0) {
								palette[3] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[3].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
//							else {
//								for(int k=3;k<5;k++) {
//									
//										//if(palette[k].GetBrightness()>0.20)
//										FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								
//									
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(palette);
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						TrainerBackSprites[p] = bytearrayBoy[p-offsetBoy];
					}					
										
					
					//l++;
					
			
					if(g1==21)
						ROM.ReplaceFile(134,TrainerBackSprites);
					else
						ROM.ReplaceFile(135,TrainerBackSprites);
			
			//overworld sprites girl
			byte[] TrainerOverworldSprites;
			if(g1==21)
				TrainerOverworldSprites = ROM.ExtractFile(209);
			else
				TrainerOverworldSprites = ROM.ExtractFile(210);
			int file71Girl = 0x7735c+0x3408;
			int file73Girl = 0x7d6cc+0x2b28;
			int file75Girl = 0x80b2c+0x8f8;
			int file81Girl = 0x85fb4+0x2248;
			int file83Girl = 0x88f6c+0xd30;
			int file89Girl = 0x90a5c+0x4c0;
			int file91Girl = 0x91d34+0xdd8;
			
			for(int p=file71Girl;p<file71Girl+32;p++)
					{
						bytearrayGirl[p-file71Girl] = TrainerOverworldSprites[p];
					}
					Color[] pallette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {
								pallette[13] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[13].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[14] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[14].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==1) {
								pallette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[7].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==2) {
								pallette[8] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[8].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[10] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[10].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								pallette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								pallette[6] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[6].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[9] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[9].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(pallette);
					
					for(int p=file71Girl;p<file71Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file71Girl];
					}
					for(int p=file73Girl;p<file73Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file73Girl];
					}
					for(int p=file75Girl;p<file75Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file75Girl];
					}
					for(int p=file81Girl;p<file81Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file81Girl];
					}
					for(int p=file83Girl;p<file83Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file83Girl];
					}
					for(int p=file89Girl;p<file89Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file89Girl];
					}
					for(int p=file91Girl;p<file91Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file91Girl];
					}
					
					
					//overworld sprite boy
					
					int file70Boy = 0x73f34+0x3408;
					int file72Boy = 0x7a784+0x2f28;
					int file74Boy = 0x80214+0x8f8;
					int file80Boy = 0x83d4c+0x2248;
					int file82Boy = 0x8821c+0xd30;
					int file88Boy = 0x9057c+0x4c0;
					int file90Boy = 0x90f3c+0xde0;
			
			for(int p=file70Boy;p<file70Boy+32;p++)
					{
						bytearrayBoy[p-file70Boy] = TrainerOverworldSprites[p];
					}
					pallette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
								pallette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								pallette[1] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[1].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
								
								
							}
							else if(j==4) {
								pallette[8] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[8].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==0) {
								pallette[14] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[14].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[15] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[15].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
//							else {
//								for(int k=10;k<13;k++) {
//									if(k!=11)
//										pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(pallette);
					
					for(int p=file70Boy;p<file70Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file70Boy];
					}
					for(int p=file72Boy;p<file72Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file72Boy];
					}
					for(int p=file74Boy;p<file74Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file74Boy];
					}
					for(int p=file80Boy;p<file80Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file80Boy];
					}
					for(int p=file82Boy;p<file82Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file82Boy];
					}
					for(int p=file88Boy;p<file88Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file88Boy];
					}
					for(int p=file90Boy;p<file90Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file90Boy];
					}
			
					if(g1==21)
						ROM.ReplaceFile(209,TrainerOverworldSprites);
					else
						ROM.ReplaceFile(210,TrainerOverworldSprites);
			}
			
			else if(g==1) {	//platinum
				byte[] TrainerBackSprites;
				TrainerBackSprites = ROM.ExtractFile(441);
			//int l=-1;
			int offsetGirl;
			byte[] bytearrayGirl = new byte[32];
			int offsetBoy;
			byte[] bytearrayBoy = new byte[32];
			
			
					offsetGirl = 0xe9a8+40;
					
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						bytearrayGirl[p-offsetGirl] = TrainerBackSprites[p];
					}
					Color[] palette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
//							if(j==0) {					
//								palette[1] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[1].GetBrightness()+Girl[j].GetBrightness())/2f);
//								palette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
//							
//							}
//							else 
							if(j==1) {
//								palette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
//								palette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
//							
							}
							else if(j==2) {
								palette[4] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[4].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[5] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[5].GetBrightness()+Girl[j].GetBrightness())/2f);
							
							}
							else if(j==3) {
								palette[11] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[11].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
							}
							else if(j==4) {
								palette[3] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[3].GetBrightness()+Girl[j].GetBrightness())/2f);
								palette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(palette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(palette);
					
					for(int p=offsetGirl;p<offsetGirl+32;p++)
					{
						TrainerBackSprites[p] = bytearrayGirl[p-offsetGirl];
					}					
										
					
					//l++;
					
					//Boy____________________________________________
						offsetBoy = 0x661c+40;
					
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						bytearrayBoy[p-offsetBoy] = TrainerBackSprites[p];
					}
					palette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
//								palette[6] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[6].GetBrightness()+Boy[j].GetBrightness())/2f);
//								palette[9] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[9].GetBrightness()+Boy[j].GetBrightness())/2f);
//								
							}
							else if(j==2) {
								
									//if(palette[k].GetBrightness()>0.20)
									palette[4] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[4].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
									
							}
							else if(j==3) {
								
									palette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
									palette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								palette[1] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[1].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[7] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[7].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==0) {
								palette[3] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[3].GetBrightness()+Boy[j].GetBrightness())/2f);
								palette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
//							else {
//								for(int k=3;k<5;k++) {
//									
//										//if(palette[k].GetBrightness()>0.20)
//										FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(palette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								
//									
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(palette);
					
					for(int p=offsetBoy;p<offsetBoy+32;p++)
					{
						TrainerBackSprites[p] = bytearrayBoy[p-offsetBoy];
					}					
										
					
					//l++;
					
			
					ROM.ReplaceFile(441,TrainerBackSprites);
			
			//overworld sprites girl
			byte[] TrainerOverworldSprites;
			TrainerOverworldSprites = ROM.ExtractFile(0x134);
			
			int file92Girl = 0x8581c+0x3408;
			int file94Girl = 0x8b78c+0x2b28;
			int file168Girl = 0xe78d8+0x2248;
			
			for(int p=file92Girl;p<file92Girl+32;p++)
					{
						bytearrayGirl[p-file92Girl] = TrainerOverworldSprites[p];
					}
					Color[] pallette = Actions.BGR555ToColor(bytearrayGirl);							
					 
					for(int j=0;j<colorChangedGirl.Length;j++)
					{
						if(colorChangedGirl[j]==true) {
							if(j==0) {
								pallette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[7].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[8] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[8].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==1) {
//								pallette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
//								pallette[7] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[7].GetBrightness()+Girl[j].GetBrightness())/2f);
//								
							}
							else if(j==2) {
								pallette[2] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[2].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[6] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[6].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								pallette[9] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[9].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[10] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[10].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
							else if(j==4) {
								pallette[12] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[12].GetBrightness()+Girl[j].GetBrightness())/2f);
								pallette[13] = FromAhsb(255,Girl[j].GetHue(),Girl[j].GetSaturation(),(pallette[13].GetBrightness()+Girl[j].GetBrightness())/2f);
								
							}
						}
					}
					
					bytearrayGirl = Actions.ColorToBGR555(pallette);
					
					for(int p=file92Girl;p<file92Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file92Girl];
					}
					for(int p=file94Girl;p<file94Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file94Girl];
					}
					for(int p=file168Girl;p<file168Girl+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayGirl[p-file168Girl];
					}					
					
					//overworld sprite boy
					
					int file91Boy = 0x823f4+0x3408;
					int file93Boy = 0x88c44+0x2f28;
					int file167Boy = 0xe5670+0x8f8;
			
			for(int p=file91Boy;p<file91Boy+32;p++)
					{
						bytearrayBoy[p-file91Boy] = TrainerOverworldSprites[p];
					}
					pallette = Actions.BGR555ToColor(bytearrayBoy);							
					 
					for(int j=0;j<colorChangedBoy.Length;j++)
					{
						if(colorChangedBoy[j]==true) {
							if(j==1) {					
//								pallette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
//								pallette[5] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[5].GetBrightness()+Boy[j].GetBrightness())/2f);
//								
							}
							else if(j==2) {
								pallette[7] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[7].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==3) {
								pallette[11] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[11].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[9] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[9].GetBrightness()+Boy[j].GetBrightness())/2f);
								
								
							}
							else if(j==4) {
								pallette[12] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[12].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[13] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[13].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
							else if(j==0) {
								pallette[1] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[1].GetBrightness()+Boy[j].GetBrightness())/2f);
								pallette[2] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[2].GetBrightness()+Boy[j].GetBrightness())/2f);
								
							}
//							else {
//								for(int k=10;k<13;k++) {
//									if(k!=11)
//										pallette[k] = FromAhsb(255,Boy[j].GetHue(),Boy[j].GetSaturation(),(pallette[k].GetBrightness()+Boy[j].GetBrightness())/2f);
//								}
//							}
						}
					}
					
					bytearrayBoy = Actions.ColorToBGR555(pallette);
					
					for(int p=file91Boy;p<file91Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file91Boy];
					}
					for(int p=file93Boy;p<file93Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file93Boy];
					}
					for(int p=file167Boy;p<file167Boy+32;p++)
					{
						TrainerOverworldSprites[p] = bytearrayBoy[p-file167Boy];
					}
			
					ROM.ReplaceFile(0x134,TrainerOverworldSprites);
			}
			
			return 0;
		}
		
		void ToolStripButton2Click(object sender, EventArgs e)
		{
			frmTrainer TrainerForm = new frmTrainer();
            TrainerForm.ShowDialog();
            Girl = TrainerForm.Girl;
            Boy = TrainerForm.Boy;
            colorChangedGirl = TrainerForm.colorChangedGirl;   
			colorChangedBoy = TrainerForm.colorChangedBoy;              
            ChangeTrainer();
		}

        private void frmMain_Load(object sender, EventArgs e)
        {

        }
    }
}
