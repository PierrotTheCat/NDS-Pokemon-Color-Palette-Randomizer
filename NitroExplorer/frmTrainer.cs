/*
 * Created by SharpDevelop.
 * User: User
 * Date: 01.02.2017
 * Time: 22:44
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace NitroExplorer
{
	/// <summary>
	/// Description of frmTrainer.
	/// </summary>
	/// 	
	public partial class frmTrainer : Form
	{
		public Color[] Girl = new Color[5];
		public bool[] colorChangedGirl = new bool[5];
		
		public Color[] Boy = new Color[7];
		public bool[] colorChangedBoy = new bool[7];
		
		public frmTrainer()
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
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button1.BackColor = colorDialog1.Color;
		    	Girl[0] = colorDialog1.Color;
		    	colorChangedGirl[0] = true;
		   	}
		}
		void Button2Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button2.BackColor = colorDialog1.Color;
		    	Girl[1] = colorDialog1.Color;
		    	colorChangedGirl[1] = true;
		   	}
		}
		void Button3Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button3.BackColor = colorDialog1.Color;
		    	Girl[2] = colorDialog1.Color;
		    	colorChangedGirl[2] = true;
		   	}
		}
		void Button4Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button4.BackColor = colorDialog1.Color;
		    	Girl[3] = colorDialog1.Color;
		    	colorChangedGirl[3] = true;
		   	}
		}
		void Button5Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button5.BackColor = colorDialog1.Color;
		    	Girl[4] = colorDialog1.Color;
		    	colorChangedGirl[4] = true;
		   	}
		}
		void Button6Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button6.BackColor = colorDialog1.Color;
		    	Boy[1] = colorDialog1.Color;
		    	colorChangedBoy[1] = true;
		   	}
		}
		void Button7Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button7.BackColor = colorDialog1.Color;
		    	Boy[2] = colorDialog1.Color;
		    	colorChangedBoy[2] = true;
		   	}
		}
		void Button8Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button8.BackColor = colorDialog1.Color;
		    	Boy[3] = colorDialog1.Color;
		    	colorChangedBoy[3] = true;
		   	}
		}
		void Button9Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button9.BackColor = colorDialog1.Color;
		    	Boy[4] = colorDialog1.Color;
		    	colorChangedBoy[4] = true;
		   	}
		}
		void Button10Click(object sender, EventArgs e)
		{
			if(colorDialog1.ShowDialog() == DialogResult.OK)
		  	{
		    	button10.BackColor = colorDialog1.Color;
		    	Boy[5] = colorDialog1.Color;
		    	colorChangedBoy[5] = true;
		   	}
		}
//		void Button11Click(object sender, EventArgs e)
//		{
//			if(colorDialog1.ShowDialog() == DialogResult.OK)
//		  	{
//		    	button11.BackColor = colorDialog1.Color;
//		    	Boy[6] = colorDialog1.Color;
//		    	colorChangedBoy[6] = true;
//		   	}
//		}
	}
}
