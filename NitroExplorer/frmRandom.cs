/*
 * Created by SharpDevelop.
 * User: User
 * Date: 01.12.2016
 * Time: 13:00
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NitroExplorer
{
	/// <summary>
	/// Description of frmRandom.
	/// </summary>
	public partial class frmRandom : Form
	{
		public int mode;
		public bool colorSelected = false;
		public Color selColor;
		
		public frmRandom()
		{
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			//
			// TODO: Add constructor code after the InitializeComponent() call.
			//
		}
		void Button1Click(object sender, EventArgs e)
		{
			if(radioButton1.Checked)		//individual modes
				mode = 1;
			else if(radioButton2.Checked)
				mode=2;
			else if(radioButton3.Checked)
				mode=3;
			else if(radioButton4.Checked)
				mode=4;
			else if(radioButton5.Checked)	//family modes
				mode=5;
			else if(radioButton6.Checked)
				mode=6;
			else if(radioButton7.Checked)
				mode=7;
			else if(radioButton8.Checked)
				mode=8;
			else if(radioButton9.Checked)	//type modes
				mode=9;
			else if(radioButton10.Checked)
				mode=10;
			else if(radioButton11.Checked)
				mode=11;
			
			Close();
		}
		void Button2Click(object sender, EventArgs e)
		{
			colorSelected = true;
			
			if(colorDialog1.ShowDialog() == DialogResult.OK)
			{
			     selColor = colorDialog1.Color;
			}
		}
	}
}
