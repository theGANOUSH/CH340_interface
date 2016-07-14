/*
 * Created by SharpDevelop.
 * User: Michal
 * Date: 7/12/2016
 * Time: 8:38 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace CH340_interface
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		private string[] ports;
		private static SerialPort COMport = null;
		public static bool connect_ready = false;
		public static bool connected = false;
		
		public static int baud;
		public static int rate;
		public static int freq;
		public static string rxadd;
		public static string txadd;
		
		public MainForm()
		{
			InitializeComponent();
			
			portcombobox.Enabled = false;
			baudcombobox.Enabled = false;
			ratecombobox.Enabled = false;
			freqcombobox.Enabled = false;
			sendbtn.Enabled = false;
			setbtn.Enabled = false;
			txtextbox.Enabled = false;

			ports = SerialPort.GetPortNames();
			foreach(string port in ports) {
				portcombobox.Items.Add(port);
			}
			portcombobox.Enabled = true;
			
		}
		void Button1Click(object sender, EventArgs e)
		{
			if(!connected && connect_ready) {
				connected = true;
				connectbtn.Text = "Disconnect";
				baud = baudcombobox.SelectedIndex;
				
				//check if any null
				if(baud == -1) baud = 2;
				
				int baudr;
				Int32.TryParse(baudcombobox.Items[baud].ToString(), out baudr);
//				Console.WriteLine("info: "+baudr+":"+baud+":"+rate+":"+freq+":"+freqcombobox.Items[freq]);
				if(connect_ready) {
					try {
//						Console.WriteLine(portcombobox.Text);
						COMport = new SerialPort(portcombobox.Text, baudr);
						COMport.BaudRate = baudr;
						COMport.Parity = Parity.None;
						COMport.StopBits = StopBits.One;
					    COMport.DataBits = 8;
					    COMport.Handshake = Handshake.None;
					    
						COMport.Open();
						COMport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
//						COMport.WriteLine("AT?\r\n");
						
						sendbtn.Enabled = true;
						setbtn.Enabled = true;
						txtextbox.Enabled = true;
					} catch (Exception ex) {
//						Console.WriteLine("error opening com port: {0}", ex.Message);
						rxtextbox.AppendText("error opening com port: "+ex.Message+"\r\n");
						connected = false;
						connectbtn.Text = "Connect";
					}
					
				}
			} else if(connected) {
				connected = false;
				connectbtn.Text = "Connect";
				//connect_ready = false;
				COMport.Close();
				sendbtn.Enabled = false;
				setbtn.Enabled = false;
				txtextbox.Enabled = false;
			}
		}
		void Button2Click(object sender, EventArgs e)
		{
			COMport.WriteLine(txtextbox.Text.ToUpper()+"\r\n");
			txtextbox.Clear();
		}
		void PortChoose(object sender, EventArgs e)
		{
			if(portcombobox.SelectedIndex != -1) {
				connectbtn.Enabled = true;
				baudcombobox.Enabled = true;
				ratecombobox.Enabled = true;
				freqcombobox.Enabled = true;
				connect_ready = true;
			} else {
				connectbtn.Enabled = false;
				baudcombobox.Enabled = false;
				ratecombobox.Enabled = false;
				freqcombobox.Enabled = false;
				COMport = null;
				connect_ready = false;
			}
		}
		void textBox_KeyPress(object sender, KeyPressEventArgs e)
	    {
			e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)
				&& (e.KeyChar != 'a') && (e.KeyChar != 'b') && (e.KeyChar != 'c') && (e.KeyChar != 'd') 
				&& (e.KeyChar != 'e') && (e.KeyChar != 'f') && (e.KeyChar != 'x');
	    }
		void enter_KeyPress(object sender, KeyPressEventArgs e)
	    {
			if(e.KeyChar == (char)Keys.Enter) Button2Click(this, new EventArgs());
	    }

		private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
		    SerialPort sp = (SerialPort)sender;
		    SetText(sp.ReadExisting());
		}
		
		delegate void SetTextCallback(string text);
		
		private void SetText(string text)
		{
			if(this.rxtextbox.InvokeRequired) {
				SetTextCallback d = new SetTextCallback(SetText);
				this.Invoke(d, new object[] { text });
			} else {
				this.rxtextbox.AppendText(text);
//				Console.WriteLine(text);
			}
		}
		void SetbtnClick(object sender, EventArgs e)
		{
			rate = ratecombobox.SelectedIndex+1;
			freq = freqcombobox.SelectedIndex;
			
			if(string.IsNullOrEmpty(rxaddr.Text)) rxadd = "0xff,0xff,0xff,0xff,0xff";
			else rxadd = "0x"+rxaddr.Text[0]+rxaddr.Text[1]+",0x"+rxaddr.Text[2]+rxaddr.Text[3]+",0x"+rxaddr.Text[4]+rxaddr.Text[5]+",0x"+rxaddr.Text[6]+rxaddr.Text[7]+",0x"+rxaddr.Text[8]+rxaddr.Text[9];
			if(string.IsNullOrEmpty(txaddr.Text)) txadd = "0xff,0xff,0xff,0xff,0xff";
			else txadd = "0x"+txaddr.Text[0]+txaddr.Text[1]+",0x"+txaddr.Text[2]+txaddr.Text[3]+",0x"+txaddr.Text[4]+txaddr.Text[5]+",0x"+txaddr.Text[6]+txaddr.Text[7]+",0x"+txaddr.Text[8]+txaddr.Text[9];

			if(rate == -1) rate = 2;
			if(freq == -1) freq = 0;

			rxtextbox.AppendText("Setting\r\n");
			COMport.WriteLine("AT+RATE="+rate+"\r\n");
			Thread.Sleep(500);
			COMport.WriteLine("AT+FREQ="+freqcombobox.Items[freq]+"G\r\n");
			Thread.Sleep(500);
			COMport.WriteLine("AT+RXA="+rxadd+"\r\n");
			Thread.Sleep(500);
			COMport.WriteLine("AT+TXA="+txadd+"\r\n");
			
//			Console.WriteLine("AT+RATE="+rate+"\r\n");
//			Console.WriteLine("AT+FREQ="+freqcombobox.Items[freq]+"G\r\n");
//			Console.WriteLine("AT+RXA="+rxadd+"\r\n");
//			Console.WriteLine("AT+TXA="+txadd+"\r\n");
		}
		void RescanbtnClick(object sender, EventArgs e)
		{
			ports = SerialPort.GetPortNames();
			foreach(string port in ports) {
				if(!portcombobox.Items.Contains(port)) portcombobox.Items.Add(port);
			}
			portcombobox.Enabled = true;
		}
	}
}
