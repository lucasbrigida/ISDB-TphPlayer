﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using ISDB_TphPlayer.Utils;

namespace ISDB_TphPlayer
{
    public partial class MainForm : Form
    {
        private Dictionary<string, List<string[]>> channelInfoList;
        private List<string[]> epgData;
        private bool autoFullScreen = false;
        private bool enableEPG = false;
        private string bandwidth = "";
        private int currentIndexPlaying = -1;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadSettings(SettingsHandler.LoadSettings());
            LoadChannelsToList();
        }

        private void channelListBox_DoubleClick(object sender, EventArgs e)
        {
            PlaySelectedChannel();
        }

        private void channelListBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if(e.KeyChar == Convert.ToInt16(Keys.Enter))
                PlaySelectedChannel();
        }

        private void scanButton_Click(object sender, EventArgs e)
        {
            ScanForm scanForm = new ScanForm();
            bool isPlaying = axVLCPlugin21.playlist.isPlaying;
            axVLCPlugin21.playlist.stop();
            scanForm.ShowDialog();
            if (!scanForm.didScanChannels && isPlaying)
                axVLCPlugin21.playlist.playItem(currentIndexPlaying);
            LoadChannelsToList();
        }

        private void optionButton_Click(object sender, EventArgs e)
        {
            VideoOptions videoForm = new VideoOptions();
            var result = videoForm.ShowDialog();
            if (result == DialogResult.OK)
            {
                LoadSettings(videoForm.settings);
            }
        }

        private void LoadChannelsToList()
        {
            channelInfoList = ChannelInfoHandler.GetChannelInfoList();
            channelListBox.Items.Clear();
            if(axVLCPlugin21.playlist.itemCount > 0) axVLCPlugin21.playlist.items.clear();
            if (channelInfoList == null) return;

            foreach (KeyValuePair<string, List<string[]>> channelInfo in channelInfoList)
            {
                foreach (string[] channelData in channelInfo.Value)
                {
                    string[] vOptions = { @":dvb-frequency=" + channelInfo.Key, @":dvb-bandwidth=0", @":program=" + channelData[0]};
                    axVLCPlugin21.playlist.add(@"dvb-t://", null, vOptions);
                    channelListBox.Items.Add(channelData[1]);
                }
            }
        }

        private void LoadSettings(List<string[]> settings)
        {
            foreach (string[] setting in settings)
            {
                switch (setting[0])
                {
                    case "aspect_ratio": axVLCPlugin21.video.aspectRatio = setting[1]; break;
                    case "auto_fullscreen": autoFullScreen = Boolean.Parse(setting[1]); break;
                    case "enable_epg": enableEPG = Boolean.Parse(setting[1]); break;
                    case "default_bandwidth": bandwidth = setting[1]; break;
                }
            }
        }

        private void PlaySelectedChannel()
        {
            if (channelListBox.SelectedItem != null)
            {
                if (channelListBox.SelectedItem.ToString().Length != 0)
                {
                    epgListView.Items.Clear();
                    if (enableEPG)
                    {
                        string targetFrequency = channelInfoList.FirstOrDefault(x => x.Value.Where(y => y[1] == channelListBox.SelectedItem.ToString()).Count() > 0).Key;
                        string serviceId = channelInfoList[targetFrequency].SingleOrDefault(x => x[1] == channelListBox.SelectedItem.ToString())[0];
                        axVLCPlugin21.playlist.stop();
                        epgData = ChannelInfoHandler.GetEPGData(targetFrequency, bandwidth, serviceId);
                        if (epgData != null)
                        {
                            foreach (string[] epg in epgData)
                            {
                                epgListView.Items.Add(epg[0]);
                                epgListView.Items[epgListView.Items.Count - 1].SubItems.Add(channelListBox.SelectedItem.ToString());
                                epgListView.Items[epgListView.Items.Count - 1].SubItems.Add(epg[1]);
                                epgListView.Items[epgListView.Items.Count - 1].SubItems.Add(epg[2]);
                                epgListView.Items[epgListView.Items.Count - 1].SubItems.Add(epg[5]);
                                epgListView.Items[epgListView.Items.Count - 1].SubItems.Add(epg[6]);
                            }
                        }
                    }
                    currentIndexPlaying = channelListBox.SelectedIndex;
                    axVLCPlugin21.playlist.playItem(channelListBox.SelectedIndex);
                    axVLCPlugin21.video.fullscreen = autoFullScreen;
                }
            }
        }


    }
}
