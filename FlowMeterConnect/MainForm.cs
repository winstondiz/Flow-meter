﻿using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using FlowMeterLibr;
using FlowMeterLibr.Enums;
using FlowMeterLibr.Events;
using FlowMeterLibr.Structs;
using FlowMeterLibr.Сommunication;
using FlowMeterLibr.TO;
using HidLibrary;
using log4net;

namespace FlowMeterConnect
{
    public partial class MainForm : Form
    {
        private bool _connect;

        private static readonly ILog Log = Program.log;
        private static HidDevice _device;
        private static readonly FlowMeterManager _flowMeterManager = new FlowMeterManager();
        private FlowUITabs _currentTab;
        private FlowUITabs _prevTab;
        private FlowTypeWork _stateFlowWork;

        public MainForm()
        {
            InitializeComponent();

            timerConnect.Interval = 1000;
            timerConnect.Tick += TimerConnectOnTick;
            timerConnect.Start();

            timerUpdate.Interval = 1000;
            timerUpdate.Tick += TimerUpdateOnTick;
            _prevTab = FlowUITabs.None;
        }

        private void TimerUpdateOnTick(object sender, EventArgs eventArgs)
        {
            switch (_currentTab)
            {
                case FlowUITabs.DefaultTab:
                    if (_prevTab != _currentTab)
                    {
                        _device.SendDataToDevice(FlowCommands.DeviceInfo);
                    }
                    _device.SendDataToDevice(FlowCommands.RtcTime);
                    break;
                case FlowUITabs.SettingTab:
                    if (_prevTab == FlowUITabs.DefaultTab)
                        _device.SendDataToDevice(FlowCommands.DeviceInfoStop);
                    break;
                case FlowUITabs.ServiceTab:
                    if (_prevTab == FlowUITabs.DefaultTab)
                        _device.SendDataToDevice(FlowCommands.DeviceInfoStop);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            _prevTab = _currentTab;
        }

        private void TimerConnectOnTick(object sender, EventArgs eventArgs)
        {
            _connect = ConnectToDevice();
            if (!_connect) return;
            Log.Debug("Device conneted from timer");
            timerUpdate.Start();
            timerConnect.Stop();
        }


        private void InvokeCustomEvents()
        { 
            _flowMeterManager.TimeChange += FlowMeterManagerOnTimeChange;
            _flowMeterManager.ConfigGet += FlowMeterManagerOnConfigGet;
            _flowMeterManager.CommonInfoGet += FlowMeterManagerOnCommonInfoGet;
            _flowMeterManager.TypeWork += FlowMeterManagerOnTypeWork;
        }

        private void FlowMeterManagerOnTypeWork(object sender, FlowMeterWorkStatusEventsArgs flowMeterWorkStatusEventsArgs)
        {
           // Debug.WriteLine("[GET] Type");
            Log.Debug("[GET] Type");
            _stateFlowWork = flowMeterWorkStatusEventsArgs.TypeWork;
            var strToChangeType = flowMeterWorkStatusEventsArgs.TypeWork.GetDescription();    
            if (InvokeRequired)
            {
                labelTypeOfWork.Invoke(new Action(() =>
                {
                    labelTypeOfWork.Text = strToChangeType;
                }));
            }
            else
            {
                labelTypeOfWork.Text = strToChangeType;
            }
        }

        private void FlowMeterManagerOnConfigGet(object sender, FlowMeterEventArgs flowMeterEventArgs)
        {
            //Debug.WriteLine("[GET] Config");
            Log.Debug("[GET] Config");
        }

        private void FlowMeterManagerOnTimeChange(object sender, FlowMeterEventArgs flowMeterEventArgs)
        {
           // Debug.WriteLine("[GET] Time");
            Log.Debug("[GET] Time");
           if (InvokeRequired)
           {
                Invoke(new Action(() =>
                {
                    textBox7.Text = flowMeterEventArgs.State.DateTime.ConvertedDateTime.ToString();
                }));
            }
           else
           {
                textBox7.Text = flowMeterEventArgs.State.DateTime.ConvertedDateTime.ToString();
            }
        }

        private void FlowMeterManagerOnCommonInfoGet(object sender, FlowMeterEventArgs flowMeterEventArgs)
        {
            //Debug.WriteLine("[GET] Common");
            if (InvokeRequired)
            {
                Invoke(new Action(() =>
                {
                    var flowStruct = flowMeterEventArgs.State.DevInfo.FlowStruct;
                    qCurrentTextBox.Text = flowStruct.QCurrent1.ToString();
                    vModule1TextBox.Text = flowStruct.VModule1.ToString();
                    vPlusTextBox.Text = flowStruct.VPlus1.ToString();
                    vMinusTextBox.Text = flowStruct.VMinus1.ToString();
                    teTimeTextBox.Text = flowStruct.TeTime1.ToString();
                    tpTimeTextBox.Text = flowStruct.TpTime1.ToString();
                    deviceCrcTextBox.Text = flowStruct.DeviceCrc1.IntTohHexString();
                    deviceSerialTextBox.Text = flowStruct.DeviceSerial.ToString();
                    firmwareNameTextBox.Text = flowStruct.FirmwareName;
                }));
            }
            else
            {
                var flowStruct = flowMeterEventArgs.State.DevInfo.FlowStruct;
                qCurrentTextBox.Text = flowStruct.QCurrent1.ToString();
                vModule1TextBox.Text = flowStruct.VModule1.ToString();
                vPlusTextBox.Text = flowStruct.VPlus1.ToString();
                vMinusTextBox.Text = flowStruct.VMinus1.ToString();
                teTimeTextBox.Text = flowStruct.TeTime1.ToString();
                tpTimeTextBox.Text = flowStruct.TpTime1.ToString();
                deviceCrcTextBox.Text = flowStruct.DeviceCrc1.IntTohHexString();
                deviceSerialTextBox.Text = flowStruct.DeviceSerial.ToString();
            }
        }

        private bool ConnectToDevice()
        {
            if (_flowMeterManager.OpenDevice())
            {
                _flowMeterManager.DeviceAttached += FlowMeterManagerOnDeviceAttached;
                _flowMeterManager.DeviceRemoved += FlowMeterManagerOnDeviceRemoved;

                InvokeCustomEvents();

                Log.Debug("[FlowMate] found");
                Debug.WriteLine("FlowMate found");
                return true;
            }
            Debug.WriteLine("Could not find a FlowMate.");
            return false;
        }


        private void FlowMeterManagerOnDeviceRemoved(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, EventArgs>(FlowMeterManagerOnDeviceRemoved), sender, eventArgs);
                return;
            }
            _connect = false;
            Log.Debug("Device disconect");
            Debug.WriteLine("FlowMeter removed.");
        }

        private void FlowMeterManagerOnDeviceAttached(object sender, EventArgs eventArgs)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<object, EventArgs>(FlowMeterManagerOnDeviceAttached), sender, eventArgs);
                return;
            }
            _connect = true;
            _device = _flowMeterManager.device;
            Log.Debug("Device attached");
            Debug.WriteLine("FlowMeter attached.");
        }


        private void button1_Click(object sender, EventArgs e)
        {
            var message = "Установить время расходомера\nпо локальным часам?";
            var caption = "Установка времени";
            var buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = MessageBox.Show(message, caption, buttons, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _device.SendDataToDevice(FlowCommands.RtcTime, new FlowDateStruct(DateTime.Now));
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            comboBox6.Items.Clear();
            comboBox6.Items.Add("Включен");
            comboBox6.Items.Add("Отключен");
            comboBox6.SelectedIndex = comboBox6.Items.IndexOf("Включен");

            comboBox7.Items.Clear();
            comboBox7.Items.Add("Включен");
            comboBox7.Items.Add("Отключен");
            comboBox7.SelectedIndex = comboBox6.Items.IndexOf("Включен");

            comboBox3.Items.Clear();
            comboBox3.Items.Add("Импульсный");
            comboBox3.Items.Add("Частотный");
            comboBox3.Items.Add("Логический");
            comboBox3.SelectedIndex = comboBox3.Items.IndexOf("Импульсный");

            comboBox2.Items.Clear();
            comboBox2.Items.Add("Импульсный");
            comboBox2.Items.Add("Частотный");
            comboBox2.Items.Add("Логический");
            comboBox2.SelectedIndex = comboBox2.Items.IndexOf("Импульсный");
        }

        private void comboBox6_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox6.SelectedItem.ToString() == "Отключен")
            {
                comboBox3.Enabled = false;
                comboBox4.Enabled = false;
                comboBox8.Enabled = false;
                label32.Enabled = false;
                label31.Enabled = false;
                label37.Enabled = false;
                label48.Enabled = false;
                label47.Enabled = false;
                textBox26.Enabled = false;
                textBox25.Enabled = false;
            }
            else
            {
                comboBox3.Enabled = true;
                comboBox4.Enabled = true;
                comboBox8.Enabled = true;
                label32.Enabled = true;
                label31.Enabled = true;
                label37.Enabled = true;
                label48.Enabled = true;
                label47.Enabled = true;
                textBox26.Enabled = true;
                textBox25.Enabled = true;
            }
            adjustComboBox3();
        }

        private void comboBox7_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboBox7.SelectedItem.ToString() == "Отключен")
            {
                comboBox2.Enabled = false;
                comboBox5.Enabled = false;
                comboBox10.Enabled = false;
                label30.Enabled = false;
                label29.Enabled = false;
                label38.Enabled = false;
                label46.Enabled = false;
                label45.Enabled = false;
                textBox24.Enabled = false;
                textBox23.Enabled = false;
            }
            else
            {
                comboBox2.Enabled = true;
                comboBox5.Enabled = true;
                comboBox10.Enabled = true;
                label30.Enabled = true;
                label29.Enabled = true;
                label38.Enabled = true;
                label46.Enabled = true;
                label45.Enabled = true;
                textBox24.Enabled = true;
                textBox23.Enabled = true;
            }
            adjustComboBox2();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            // Initializes the variables to pass to the MessageBox.Show method.
            var message = "Калибровку необходимо запускать ТОЛЬКО при условии отсутствия течения жидкости через УПР.\n\nЗапустить сейчас?";
            var caption = "Калибровка измерительной части";
            var buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = MessageBox.Show(message, caption, buttons, MessageBoxIcon.Information);

            if (result == DialogResult.Yes)
            {
                // Calibration start.
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            // Initializes the variables to pass to the MessageBox.Show method.
            var message = "Будет выполнен возврат к\nзаводским настройкам расходомера.\n\nПродолжить?";
            var caption = "Заводские настройки";
            var buttons = MessageBoxButtons.YesNo;
            DialogResult result;

            // Displays the MessageBox.
            result = MessageBox.Show(message, caption, buttons, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                // Do factory reset.
            }
        }

        private void adjustComboBox3()
        {
            label48.Enabled = false;
            label47.Enabled = false;
            textBox26.Enabled = false;
            textBox25.Enabled = false;

            if (comboBox6.SelectedItem.ToString() == "Включен")
            {
                switch (comboBox3.SelectedItem.ToString())
                {
                    case "Импульсный":
                        label48.Enabled = true;
                        textBox26.Enabled = true;
                        label47.Enabled = true;
                        textBox25.Enabled = true;
                        break;

                    case "Частотный":
                        label48.Enabled = false;
                        textBox26.Enabled = false;
                        label47.Enabled = true;
                        textBox25.Enabled = true;
                        break;

                    case "Логический":
                        label48.Enabled = false;
                        textBox26.Enabled = false;
                        label47.Enabled = true;
                        textBox25.Enabled = true;
                        break;

                    default:
                        break;
                }
            }
        }

        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            adjustComboBox3();
        }

        private void adjustComboBox2()
        {
            label46.Enabled = false;
            label45.Enabled = false;
            textBox24.Enabled = false;
            textBox23.Enabled = false;

            if (comboBox7.SelectedItem.ToString() == "Включен")
            {
                switch (comboBox2.SelectedItem.ToString())
                {
                    case "Импульсный":
                        label46.Enabled = true;
                        textBox24.Enabled = true;
                        label45.Enabled = true;
                        textBox23.Enabled = true;
                        break;

                    case "Частотный":
                        label46.Enabled = false;
                        textBox24.Enabled = false;
                        label45.Enabled = true;
                        textBox23.Enabled = true;
                        break;

                    case "Логический":
                        label46.Enabled = false;
                        textBox24.Enabled = false;
                        label45.Enabled = true;
                        textBox23.Enabled = true;
                        break;

                    default:
                        break;
                }
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            adjustComboBox2();
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            switch (e.TabPageIndex)
            {
                case 0:
                    _currentTab = FlowUITabs.DefaultTab;
                    break;
                case 1:
                    _currentTab = FlowUITabs.SettingTab;
                    break;
                case 2:
                    _currentTab = FlowUITabs.ServiceTab;
                    break;
            }
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_currentTab == FlowUITabs.DefaultTab) //отписываемся от потоковой рассылки
                _device.SendDataToDevice(FlowCommands.DeviceInfoStop);

            if ((_device != null) && (_device.IsConnected))
                _device.CloseDevice();
        }
    }
}