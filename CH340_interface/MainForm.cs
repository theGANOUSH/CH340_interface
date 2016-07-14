/*
* Created by SharpDevelop.
* User: Michal
* Date: 7/12/2016
* Time: 8:38 PM
* 
* To change this template use Tools | Options | Coding | Edit Standard Headers.
*/
using System;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;

namespace CH340_interface
{
/// <summary>
/// Simple terminal interface, scans for ports, once connected allows
/// user to connect to device, set parameters such as baud rate, addresses,
/// etc, and send/recieve messages
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
		usernametxtbox.Enabled = true;
		
		/*
		* need to populate serial ports in dropdown when first starting
		* EDIT: removed duplicate code, now it just calls the rescan
		* button, which does the exact same thing
		*/
		RescanbtnClick(this, new EventArgs());
	}
		
	/*
	* This is needed to check whether it's already connected or not, 
	* or if the connection is ready. Some of the args from the dropdowns
	* cant be used directly but some can, so just do a little magic
	* to either extract the index number+1 (since device has non-zero
	* indeces) or the actual value such as the frequency
	* Button will toggle
	*/
	void Button1Click(object sender, EventArgs e)
	{
		if(!connected && connect_ready) {
//			connected = true;
//			connectbtn.Text = "Disconnect";
//			baud = baudcombobox.SelectedIndex;
			
			//check if any null
			if(baud == -1) baud = 2;
			
			int baudr;
			Int32.TryParse(baudcombobox.Items[baud].ToString(), out baudr);
			if(connect_ready) {
				connected = true;
				connectbtn.Text = "Disconnect";
				baud = baudcombobox.SelectedIndex;
				usernametxtbox.Enabled = false;

				try {
					COMport = new SerialPort(portcombobox.Text, baudr);
					COMport.BaudRate = baudr;
					COMport.Parity = Parity.None;
					COMport.StopBits = StopBits.One;
					COMport.DataBits = 8;
					COMport.Handshake = Handshake.None;
					
					COMport.Open();
					COMport.DataReceived += new SerialDataReceivedEventHandler(DataReceivedHandler);
					//COMport.WriteLine("AT?\r\n");
					
					sendbtn.Enabled = true;
					setbtn.Enabled = true;
					txtextbox.Enabled = true;
				} catch (Exception ex) {
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
				usernametxtbox.Enabled = true;
			}
		}
		
		/*
		* Text in box needs to be converted when doing AT commands,
		* and since I'm lazy I just make everything uppercase
		* and terminate with a carriage return and new line
		* The textbox has no history function either
		*/
		void Button2Click(object sender, EventArgs e)
		{
			if(txtextbox.Text.ToUpper().Contains("AT+") || txtextbox.Text.ToUpper().Contains("AT?")) {
//				COMport.WriteLine(txtextbox.Text.ToUpper()+"\r\n");
				SendText(txtextbox.Text.ToUpper());
			} else {
//				COMport.WriteLine(usernametxtbox.Text+":> "+txtextbox.Text+"\r\n");
				SendText(usernametxtbox.Text+":> "+txtextbox.Text);
			}
			txtextbox.Clear();
		}
		
		/*
		* if no com port then disable other boxes
		*/
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
		
		/*
		* only allows HEX characters in the address textboxes since
		* we dont want illegal characters or screw ups
		* The setting function parses this data out anyway
		*/
		void textBox_KeyPress(object sender, KeyPressEventArgs e)
		{
			e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar)
				&& (e.KeyChar != 'a') && (e.KeyChar != 'b') && (e.KeyChar != 'c') && (e.KeyChar != 'd') 
				&& (e.KeyChar != 'e') && (e.KeyChar != 'f');
		}
		
		/*
		* pressing enter in the text box is the same as
		* clicking the Tx button
		*/
		void enter_KeyPress(object sender, KeyPressEventArgs e)
		{
			if(e.KeyChar == (char)Keys.Enter) Button2Click(this, new EventArgs());
		}
		
		/*
		* simple handler for the serial port needed since data
		* will come asynchronously and needs to be written to
		* the Rx textbox
		* it's a little messy since apparantely accessing the rx textbox
		* from another thread breaks the universe so get around it
		* by using a delegate, whatever that is
		*/
		private void DataReceivedHandler(object sender, SerialDataReceivedEventArgs e)
		{
			SerialPort sp = (SerialPort)sender;
			SetText(sp.ReadExisting());
		}
		
		delegate void SetTextCallback(string text);
		
		/*
		* needed for preventing the whole thing from breaking
		* I don't actually know how this works or what it's doing
		* exactly, but I think it's making a new object from 
		* the received text, then safely passing it to the 
		* textbox? I dunno
		*/
		private void SetText(string text)
		{
			if(this.rxtextbox.InvokeRequired) {
				SetTextCallback d = new SetTextCallback(SetText);
				this.Invoke(d, new object[] { text });
			} else {
				this.rxtextbox.AppendText(text);
			}
		}
		
		/*
		* needed a quick way to set a bunch of parameters at once
		* this seemed like the best way without having to
		* do a bunch of AT commands
		* Need 0.5sec between each one just so it can breathe
		* a little
		* also checks if fields are empty and uses defaults
		*/
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
//			COMport.WriteLine("AT+RATE="+rate+"\r\n");
			SendText("AT+RATE="+rate);
			Thread.Sleep(500);
//			COMport.WriteLine("AT+FREQ="+freqcombobox.Items[freq]+"G\r\n");
			SendText("AT+FREQ="+freqcombobox.Items[freq]+"G");
			Thread.Sleep(500);
//			COMport.WriteLine("AT+RXA="+rxadd+"\r\n");
			SendText("AT+RXA="+rxadd);
			Thread.Sleep(500);
//			COMport.WriteLine("AT+TXA="+txadd+"\r\n");
			SendText("AT+TXA="+txadd);
		}
		
		/*
		* cant connect without first scanning the ports and
		* populating the device list
		* tried using a usb library to actually claim and
		* control the usb interface, but apparently these
		* devices aren't visible as usb devices and are
		* instead serial com ports, so that simplifies
		* things a lot
		* this app does also check if the com port is
		* busy or not and tells you, so if you run
		* multiple instances of this you'll see the
		* device but shouldn't be able to connect
		*/
		void RescanbtnClick(object sender, EventArgs e)
		{
			ports = SerialPort.GetPortNames();
			foreach(string port in ports) {
				if(!portcombobox.Items.Contains(port)) portcombobox.Items.Add(port);
			}
			portcombobox.Enabled = true;
		}
		
		void SendText(string text)
		{
			COMport.WriteLine(text+"\r\n");
			this.rxtextbox.AppendText(text+"\r\n");
		}
	}
}
