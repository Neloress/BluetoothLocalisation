﻿
//using Microsoft.Maui.Controls;
//using Microsoft.Maui.Storage;
using Plugin.BLE;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.Exceptions;
using Plugin.BLE.Android;
using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
//using System;
using System.Reflection;
using static Microsoft.Maui.ApplicationModel.Permissions;
//using System.Text;
//using System.Threading;

namespace AndroidBluetooth;

public partial class MainPage : ContentPage
{
	public MainPage()
	{
		InitializeComponent();

		InitValues();
	}

	private void InitValues()
	{
		ScanTime.Text = "5432";
		MeasureTime.Text = "10";
		receiveX.Text = "0";
		receiveY.Text = "0";
		receiveZ.Text = "0";

		Beacon1Adress.Text = "Nuki_2E217F4D";
		Beacon1X.Text = "723";
		Beacon1Y.Text = "635";
		Beacon1Z.Text = "92";
		Beacon1Name.Text = "Nuki";

		Beacon2Adress.Text = "ET-2820 Series";
		Beacon2X.Text = "1003";
		Beacon2Y.Text = "150";
		Beacon2Z.Text = "78";
		Beacon2Name.Text = "Drucker";

		Beacon3Adress.Text = "Smart Tag";
		Beacon3X.Text = "167";
		Beacon3Y.Text = "950";
		Beacon3Z.Text = "173";
		Beacon3Name.Text = "Tag";

		Beacon4Adress.Text = "Petkit_K3";
		Beacon4X.Text = "484";
		Beacon4Y.Text = "411";
		Beacon4Z.Text = "26";
		Beacon4Name.Text = "Petkit";

		Beacon5Adress.Text = "[TV] Samsung 7 Series (55)";
		Beacon5X.Text = "9";
		Beacon5Y.Text = "253";
		Beacon5Z.Text = "106";
		Beacon5Name.Text = "TV";

		IPpc.Text = "192.168.237.172";
		Portpc.Text = "13000";
	}

	private Beacon[] GenerateBeacons()
	{
		Beacon[] beacons = new Beacon[5];

		try 
		{
			beacons[0] = new Beacon(Beacon1Adress.Text,Beacon1X.Text,Beacon1Y.Text,Beacon1Z.Text,Beacon1Name.Text);
		} 
		catch (Exception e)
		{
			Status.Text = "Beacon 1: " + e.Message;
			return null;
		}

		try
		{
			beacons[1] = new Beacon(Beacon2Adress.Text, Beacon2X.Text, Beacon2Y.Text, Beacon2Z.Text, Beacon2Name.Text);
		}
		catch (Exception e)
		{
			Status.Text = "Beacon 2: " + e.Message;
			return null;
		}

		try
		{
			beacons[2] = new Beacon(Beacon3Adress.Text, Beacon3X.Text, Beacon3Y.Text, Beacon3Z.Text, Beacon3Name.Text);
		}
		catch (Exception e)
		{
			Status.Text = "Beacon 3: " + e.Message;
			return null;
		}

		try
		{
			beacons[3] = new Beacon(Beacon4Adress.Text, Beacon4X.Text, Beacon4Y.Text, Beacon4Z.Text, Beacon4Name.Text);
		}
		catch (Exception e)
		{
			Status.Text = "Beacon 4: " + e.Message;
			return null;
		}

		try
		{
			beacons[4] = new Beacon(Beacon5Adress.Text, Beacon5X.Text, Beacon5Y.Text, Beacon5Z.Text, Beacon5Name.Text);
		}
		catch (Exception e)
		{
			Status.Text = "Beacon 5: " + e.Message;
			return null;
		}


		return beacons;
	}

	private async void OnStartMeasuring(object sender, EventArgs e)
	{
		if (!await CheckForBluetoothPermission())
		{
			Status.Text = "No bluetooth permission";
			return;
		}

		Beacon[] beacons = GenerateBeacons();
		if (beacons == null)
			return;

		if (!double.TryParse(receiveX.Text, out double X) || !double.TryParse(receiveY.Text, out double Y) || !double.TryParse(receiveZ.Text, out double Z))
		{
			Status.Text = "Wrong receiver Pos";
			return;
		}



		var ble = CrossBluetoothLE.Current;
		var adapter = CrossBluetoothLE.Current.Adapter;

		int scanTime;
		if (int.TryParse(ScanTime.Text, out scanTime))
		{
			if (scanTime > 0 && scanTime <= 60000)
				adapter.ScanTimeout = scanTime;
			else
			{
				Status.Text = "Scan time wrong";
				return;
			}
		}
		else
		{
			Status.Text = "Scan time wrong";
			return;
		}

		int measureTime;
		if (int.TryParse(MeasureTime.Text, out measureTime))
		{
			if (measureTime > 0 && measureTime <= 600)
				adapter.ScanTimeout = scanTime;
			else
			{
				Status.Text = "Meassure time wrong";
				return;
			}
		}
		else
		{
			Status.Text = "Meassure time wrong";
			return;
		}

		List<IDevice> deviceList = new List<IDevice>();
		adapter.ScanMode = ScanMode.LowLatency;//ScanMode.Passive;
		adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);

		int steps = (measureTime *60* 1000) / scanTime;

		string mainDir = FileSystem.Current.AppDataDirectory;
		string fileDir = Path.Combine(mainDir, GetTimeStamp() + "__data.csv");

		using (StreamWriter sw = File.CreateText(fileDir))
		{
			sw.WriteLine("ScanTime: " + scanTime);
			sw.WriteLine("ReceiverPos: " + X.ToString(CultureInfo.InvariantCulture)+ " "+Y.ToString(CultureInfo.InvariantCulture)+ " "+Z.ToString(CultureInfo.InvariantCulture));
			sw.WriteLine();
			sw.WriteLine("Time;" + beacons[0].ToString()+";" + beacons[1].ToString() + ";" + beacons[2].ToString()+ ";" + beacons[3].ToString() + ";" + beacons[4].ToString());
			for (int i = 0; i < steps; i++)
			{
				deviceList.Clear();
				await adapter.StartScanningForDevicesAsync();
				sw.WriteLine(GetRow(deviceList,beacons));
				Status.Text = "Meassured "+(i+1)+" of "+steps;
			}
		}

		Status.Text = "Meassuring finished";
	}
	private string GetRow(List<IDevice> deviceList, Beacon[] beacons)
	{
		string row = GetTimeStamp()+";";
		for (int i = 0; i < 5; i++)
		{ 
			Beacon beacon = beacons[i];
			bool found = false;
			int rssi = int.MinValue;
			foreach (IDevice device in deviceList)
			{
				object obj = device.NativeDevice;
				PropertyInfo propInfo = obj.GetType().GetProperty("Address");
				string address = (string)propInfo.GetValue(obj, null);
				if (address == beacon.AddressOrName || (device.Name!=null && device.Name.Contains(beacon.AddressOrName)))
				{
					if(device.Rssi>rssi)
						rssi = device.Rssi;
					found = true;
				}
			}
			if (found)
			{
				row += rssi + ";";
			}
			else
			{
				row += "noVal;";
			}
		}
		return row;
	}

	internal static string GetTimeStamp()
	{
		DateTime dateTime = DateTime.Now;

		return dateTime.Year + "." + dateTime.Month + "." + dateTime.Day + "_" + dateTime.Hour + "-" + dateTime.Minute + "-" + dateTime.Second + "_" + dateTime.Millisecond;
	}

	private async void OnSendResults(object sender, EventArgs e)
	{
		if (!await CheckForNetworkPermission())
		{
			Status.Text = "No network permission";
			return;
		}

		string message = "";

		string mainDir = FileSystem.Current.AppDataDirectory;
		foreach (FileInfo fileInfo in (new DirectoryInfo(mainDir)).GetFiles())
		{
			string temp = File.ReadAllText(fileInfo.FullName);
			if (message != "")
				message += "$";

			message += fileInfo.Name;
			message += "&";
			message += temp;
		}

		try
		{
			var ipEndPoint = new IPEndPoint(IPAddress.Parse(IPpc.Text), int.Parse(Portpc.Text));
			using TcpClient tcpClient = new();
			tcpClient.Connect(ipEndPoint);

			// Prefer using declaration to ensure the instance is Disposed later.
			//using TcpClient client = new TcpClient(IPpc.Text, int.Parse(Portpc.Text));
			// Get a client stream for reading and writing.
			NetworkStream stream = tcpClient.GetStream();

			Byte[] data = System.Text.Encoding.ASCII.GetBytes(message + "%");
			stream.Write(data, 0, data.Length);
		}
		catch (Exception error)
		{
			Status.Text = "Send Error; "+ error.Message;
			return;
		}
		
		Status.Text = "Results send";
	}

	private async void OnDeleteResults(object sender, EventArgs e)
	{
		if (ClearCheck.Text == "delete")
		{
			ClearCheck.Text = "";

			string mainDir = FileSystem.Current.AppDataDirectory;
			foreach (FileInfo fileInfo in (new DirectoryInfo(mainDir)).GetFiles())
			{ 
				try { fileInfo.Delete(); } catch { }
			}
		}
		else
		{
			Status.Text = "Deletion not possible";
			return;
		}

		Status.Text = "Results deleted";
	}

	//private async void CollectData(int stepDuration, List<Tuple<long, int>> values)
	//{
	//	var ble = CrossBluetoothLE.Current;
	//	var adapter = CrossBluetoothLE.Current.Adapter;


	//	List<IDevice> deviceList = new List<IDevice>();

	//	adapter.ScanTimeout = stepDuration;
	//	adapter.ScanMode = ScanMode.LowLatency;//ScanMode.Passive;
	//	adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);

	//	while (_runSearch)
	//	{
	//		//deviceList = new List<IDevice>();
	//		//adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
	//		await adapter.StartScanningForDevicesAsync();

	//		foreach (IDevice device in deviceList)
	//		{
	//			//ParcelUuid[] uuids = device.GetUuids();
	//			if (device.Name != null && device.Name.Contains("Smart Tag"))
	//			{

	//				try
	//				{
	//					await adapter.ConnectToDeviceAsync(device);
	//					var services = await device.GetServicesAsync();
	//				}
	//				catch (DeviceConnectionException e)
	//				{
	//					// ... could not connect to device
	//				}


	//				int value = device.Rssi;
	//				long time = System.DateTime.Now.Ticks;
	//				_values.Add(new Tuple<long, int>(time, value));
	//			}
	//		}
	//	}
	//}
	//private async void OnCounterClicked(object sender, EventArgs e)
	//{
	//	//_runSearch = false;

	//	//if (Tag != null)
	//	//{


	//	//	//while (!await Tag.UpdateRssiAsync()) ;

	//	//	bool rssiUpdateResult = await Tag.UpdateRssiAsync();
	//	//	int value = Tag.Rssi;

	//	//	CounterBtn.Text = $""+value;
	//	//}
	//	//else
	//	//{
	//	//	CounterBtn.Text = $"No Tag";
	//	//}


	//	//SemanticScreenReader.Announce(CounterBtn.Text);
	//}

	private async Task<bool> CheckForBluetoothPermission()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();

		if (status == PermissionStatus.Granted)
			return true;

		if (Permissions.ShouldShowRationale<Permissions.LocationAlways>())
		{
			await Shell.Current.DisplayAlert("Needs permissions", "BECAUSE!!!", "OK");
		}

		status = await Permissions.RequestAsync<Permissions.LocationAlways>();

		return status == PermissionStatus.Granted;
	}

	private async Task<bool> CheckForNetworkPermission()
	{
		var status = await Permissions.CheckStatusAsync<Permissions.NetworkState>();

		if (status == PermissionStatus.Granted)
			return true;

		if (Permissions.ShouldShowRationale<Permissions.NetworkState>())
		{
			await Shell.Current.DisplayAlert("Needs permissions", "BECAUSE!!!", "OK");
		}

		status = await Permissions.RequestAsync<Permissions.NetworkState>();

		return status == PermissionStatus.Granted;
	}

	//private async Task<Tuple<bool, int>> Measure(int timeOut)
	//{
	//	var ble = CrossBluetoothLE.Current;
	//	var adapter = CrossBluetoothLE.Current.Adapter;


	//	List<IDevice> deviceList = new List<IDevice>();

	//	adapter.ScanTimeout = timeOut;
	//	adapter.ScanMode = ScanMode.LowLatency;//ScanMode.Passive;



	//	adapter.DeviceDiscovered += (s, a) => deviceList.Add(a.Device);
	//	await adapter.StartScanningForDevicesAsync();




	//	foreach (IDevice device in deviceList)
	//	{
	//		//BluetoothDevice test = (BluetoothDevice)device.NativeDevice;

	//		object obj = device.NativeDevice;
	//		PropertyInfo propInfo = obj.GetType().GetProperty("Address");
	//		string address = (string)propInfo.GetValue(obj, null);

	//		//if (address== _blackTag)
	//		//{
	//		//	return new Tuple<bool, int>(true, device.Rssi);
	//		//}

	//		if (device.Name != null && device.Name.Contains("Smart Tag"))
	//		{
	//			//return new Tuple<bool, int>(true, device.Rssi);
	//			List<string> test = new List<string>();

	//			int streng = device.Rssi;

				

	//			try
	//			{
	//				await adapter.ConnectToDeviceAsync(device);
	//				var services = await device.GetServicesAsync();

	//				while (true)
	//				{
	//					bool rssiUpdateResult = await device.UpdateRssiAsync();
	//					int streng2 = device.Rssi;
	//				}

	//				foreach (IService service in services)
	//				{
	//					var characteristics = await service.GetCharacteristicsAsync();
	//					foreach (Characteristic characteristic in characteristics)
	//					{
	//						bool found = false;
	//						foreach (string s in test)
	//						{
	//							if (s == characteristic.Uuid)
	//								found = true;
	//						}
	//						if (!found)
	//							test.Add(characteristic.Uuid);
	//					}
	//				}

	//				string all = "";
	//				foreach (string s in test)
	//				{
	//					all += s + "\n";
	//				}

	//				string mainDir = FileSystem.Current.AppDataDirectory;
	//				string cacheDir = FileSystem.Current.CacheDirectory;

	//				//string FileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "myfile1.txt");
	//				string FileName = Path.Combine(mainDir, "myfile1.txt");

	//				string[] testsets = File.ReadAllLines(FileName);

	//				int asd = 0;
	//				for (int i = 0; i < testsets.Length; i++)
	//				{
	//					foreach (string s in test)
	//					{
	//						if (s == testsets[i])
	//							asd++;
	//					}
	//				}
	//				//using (StreamWriter sw = File.CreateText(FileName))
	//				//{
	//				//	foreach (string s in test)
	//				//	{
	//				//		sw.WriteLine(s);
	//				//	}
	//				//}

	//			}
	//			catch (DeviceConnectionException e)
	//			{
	//				// ... could not connect to device
	//			}
	//		}
	//	}
	//	return new Tuple<bool, int>(false, -1);
	//}

	//private async void BluetoothDetection_Clicked(object sender, EventArgs e)
	//{
	//	if (!await CheckForBluetoothPermission())
	//		throw new Exception("No Permision");

	//	//_runSearch = true;

	//	//_values = new List<Tuple<long, int>>();
	//	//Thread thread = new Thread(() => CollectData(1000, _values));
	//	//thread.Start();

	//	//Thread.Sleep(100000);
	//	//_runSearch = false;
	//	//thread.Join();

	//	//int stop = 56;

	//	//var ble = CrossBluetoothLE.Current;
	//	//var adapter = CrossBluetoothLE.Current.Adapter;


	//	//List<IDevice> deviceList2 = new List<IDevice>();

	//	//adapter.ScanTimeout = 1000;
	//	//adapter.ScanMode = ScanMode.LowLatency;//ScanMode.Passive;



	//	////adapter.DeviceAdvertised += (s, a) => deviceList1.Add(a.Device);
	//	//adapter.DeviceDiscovered += (s, a) => deviceList2.Add(a.Device);
	//	//await adapter.StartScanningForDevicesAsync();




	//	//foreach (IDevice device in deviceList2)
	//	//{
	//	//	if (device.Name != null && device.Name.Contains("Smart Tag"))
	//	//	{
	//	//		int stop3 = 23;// Tag = device;
	//	//	}
	//	//}

	//	List<Tuple<long, int>> values = new List<Tuple<long, int>>();
	//	for (int i = 0; i < 10; i++)
	//	{
	//		Tuple<bool, int> tuple = await Measure(10000);
	//		if (tuple.Item1)
	//		{
	//			long time = System.DateTime.Now.Ticks;
	//			values.Add(new Tuple<long, int>(time, tuple.Item2));
	//		}
	//	}

	//}
}

