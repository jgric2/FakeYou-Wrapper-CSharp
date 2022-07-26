using NAudio.Wave;
using SoxSharp;
using SoxSharp.Effects;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FakeYou_Wrapper.FakeYouCSharp;

namespace FakeYou_Wrapper
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        public List<VoiceModel> availableVoices = new List<VoiceModel>();
        public List<VoiceModel> CurrentList = new List<VoiceModel>();
        byte[] currentTTSDataWav = null;

        private void Form1_Load(object sender, EventArgs e)
        {
            availableVoices = FakeYouCSharp.GetListOfVoices();
            CurrentList = new List<VoiceModel>();
            foreach (var item in availableVoices)
            {
                CurrentList.Add(item);
            }

            foreach (var item in CurrentList)
            {
                listBox1.Items.Add(item.title + " : " + item.creator_display_name);
            }
        }


        public class HolderTemp
        {
            public int index = 0;
            public byte[] data;
        }


        private void button3_Click(object sender, EventArgs e)
        {
            string inputtext = textBox2.Text;
            List<string> inputparsed = new List<string>();
            while (inputtext.Length > numericUpDownJoinLength.Value)
            {
                string currentTest = inputtext.Remove(0, (int)numericUpDownJoinLength.Value);
                if (currentTest.Contains(" "))
                {
                    int ind = currentTest.IndexOf(" ");
                    string currentM = inputtext.Substring(0, (int)numericUpDownJoinLength.Value + ind);
                    inputtext = inputtext.Remove(0, (int)numericUpDownJoinLength.Value + ind);
                    inputparsed.Add(currentM);
                }
            }
            inputparsed.Add(inputtext);

            List<byte[]> WavDataList = new List<byte[]>();
            var primeNumbers = new ConcurrentBag<HolderTemp>();
            object lockCurrent = new object();
            Console.WriteLine("INPUT COUNT: " + inputparsed.Count.ToString());
            List<Task> Tasks = new List<Task>();

            for (int index = 0; index < inputparsed.Count; index++)
            {
               // Console.WriteLine(index.ToString() + " " + "INIT 1");
                string modeltok = "";
                this.Invoke((MethodInvoker)delegate
                {
                    modeltok = CurrentList[listBox1.SelectedIndex].model_token;
                });
               // Console.WriteLine(index.ToString() + " " + "INIT 2");
                string textM = inputparsed[index];
                var ind = index;
                Tasks.Add(Task.Run(() =>
                {
                    //Console.WriteLine(ind.ToString() + " " + "INIT 1");
                    var ReqM = FakeYouCSharp.MakeTTSRequest(modeltok, textM);
                    //Console.WriteLine(ind.ToString() + " " + "INIT 3");
                    if (ReqM != "Failed")
                    {
                        //Console.WriteLine(ind.ToString() + " " + "INIT 4");
                        var poll = PollTTSRequestStatus(ReqM);
                        while (poll.status == "started" | poll.status == "pending")
                        {
                            Thread.Sleep(100);
                            poll = PollTTSRequestStatus(ReqM);
                           //Console.WriteLine(ind.ToString() + " " + "INIT 5");
                        }
                        var bytes = FakeYouCSharp.StreamTTSAudioClip(FakeYouCSharp.m_cdn + poll.maybe_public_bucket_wav_audio_path, false);
                        HolderTemp hhh = new HolderTemp();
                        hhh.index = ind;
                        hhh.data = bytes;
                        primeNumbers.Add(hhh);
                        //Console.WriteLine(ind.ToString() + " " + "INIT 6");
                    }
                }));
            }
            Task.WaitAll(Tasks.ToArray());

            var tempL = primeNumbers.ToList();
            tempL = tempL.OrderBy(x => x.index).ToList();

            List<byte[]> outb = new List<byte[]>();
            foreach (var item in tempL)
            {
                outb.Add(item.data);
            }
                (double[] audio, int sampleRate) = ReadWav(outb[0]);//("fileSilence.wav");
                List<double[]> posPotentail = new List<double[]>();

                double threshDef = 0.008;
                double thresh = 0.008;
                int currentstart = 0;
                int currentend = 0;
                int currentcount = 0;
                for (int i = 0; i < audio.Length; i++)
                {
                    if (audio[i] < thresh)
                    {
                        if (currentstart == 0)
                        {
                            currentstart = i;
                            currentend = 0;
                            currentcount = 0;
                            thresh = threshDef * 8;
                        }
                        else
                        {
                            currentcount++;
                        }
                    }
                    else if (audio[i] > -thresh && audio[i] <= 0)
                    {
                        if (currentstart == 0)
                        {
                            currentstart = i;
                            currentend = 0;
                            currentcount = 0;
                            thresh = threshDef * 8;
                        }
                        else
                        {
                            currentcount++;
                        }
                    }
                    else
                    {
                        if (currentstart > 0)
                        {
                            currentend = i;
                            double[] ent = new double[3] { currentstart, currentend, currentcount };
                            currentcount = 0;
                            currentend = 0;
                            currentstart = 0;
                            thresh = threshDef;
                            posPotentail.Add(ent);
                        }
                    }
                }


                double longest = 0;
                double find = 0;
                double[] outt = new double[3];
                for (int i = 0; i < posPotentail.Count; i++)
                {
                    if (posPotentail[i][2] > longest)
                    {
                        longest = posPotentail[i][2];
                        find = i;
                        outt = posPotentail[i];
                    }
                }
                outt[0] = (outt[0] / 32000);
                outt[1] = (outt[1] / 32000);
                outt[2] = (outt[2] / 32000);

                //REMOVE NOISE

                using (var sox = new Sox("sox.exe"))
                {
                    sox.Output.Type = FileType.WAV;
                    var tsS = TimeSpan.FromSeconds(outt[0]);
                    var tsE = TimeSpan.FromSeconds(outt[2]);

                    var posstart = new SoxSharp.Effects.Types.Position(tsS);
                    var posend = new SoxSharp.Effects.Types.Position(tsE);
                    sox.Effects.Add(new TrimEffect(posstart, posend));


                    sox.Process("tempwav.wav", "fileSilenceCut.wav");
                }
                File.Delete("tempwav.wav");
                using (var sox = new Sox("sox.exe"))
                {
                    sox.Output.Type = FileType.WAV;
                    var posstart = new SoxSharp.Effects.Types.Position((uint)outt[0]);
                    var posend = new SoxSharp.Effects.Types.Position((uint)outt[1]);
                    sox.Effects.Add(new NoiseProfileEffect("noiseprof.prof"));
                    //sox.Effects.Add(new NoiseReductionEffect("fileSilenceCut.wav")) ;
                    sox.Process("fileSilenceCut.wav");
                }

            Concatenate(outb, Application.StartupPath + "\\file.wav");
            using (var sox = new Sox("sox.exe"))
            {
                sox.Output.Type = FileType.WAV;
                sox.Effects.Add(new NoiseReductionEffect("noiseprof.prof", 0.05));
                sox.Process("file.wav", "fileDONE.wav");
            }

            System.Media.SoundPlayer player = new System.Media.SoundPlayer(Application.StartupPath + "\\fileDONE.wav");
            player.Play();

        }


        public static void Concatenate(List<byte[]> sourceByteList, string outputFile)
        {
            byte[] buffer = new byte[1024];
            WaveFileWriter waveFileWriter = null;
            try
            {
                foreach (byte[] sourceFile in sourceByteList)
                {
                    using (var WavStream = new MemoryStream(sourceFile))
                    {
                        using (var reader = new WaveFileReader(WavStream))
                        {
                            if (waveFileWriter == null)
                            {
                                // first time in create new Writer
                                waveFileWriter = new WaveFileWriter(outputFile, reader.WaveFormat);
                            }
                            else
                            {
                                if (!reader.WaveFormat.Equals(waveFileWriter.WaveFormat))
                                {
                                    throw new InvalidOperationException("Can't concatenate WAV Files that don't share the same format");
                                }
                            }
                            int read;
                            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                waveFileWriter.WriteData(buffer, 0, read);
                            }
                        }
                    }
                }
            }
            finally
            {
                if (waveFileWriter != null)
                {
                    waveFileWriter.Dispose();
                }
            }
        }


        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            CurrentList = new List<VoiceModel>();
            listBox1.Items.Clear();
            CurrentList = availableVoices.Where(x => x.title.ToUpper().Contains(textBox1.Text.ToUpper())).ToList();
            foreach (var item in CurrentList)
            {
                listBox1.Items.Add(item.title + " : " + item.creator_display_name);
            }
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\file.wav"))
            {
                SoundPlayer player = new SoundPlayer(Application.StartupPath + "\\File.wav");
                player.PlaySync();
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            this.Invoke((MethodInvoker)delegate
            {
                Stopwatch stpw = new Stopwatch();
                stpw.Start();

                if (listBox1.SelectedIndex == -1)
                {
                    MessageBox.Show("Please select a voice!");
                    return;
                }
                if (textBox2.Text.Length < 1)
                {
                    MessageBox.Show("Please include text to speak!");
                    return;
                }

                labelStatus.Text = "";
                buttonPlay.Enabled = false;
                Application.DoEvents();
                var Req = FakeYouCSharp.MakeTTSRequest(CurrentList[listBox1.SelectedIndex].model_token, textBox2.Text);
                if (Req != "Failed")
                {

                    var poll = PollTTSRequestStatus(Req);
                    DateTime LastCheck = DateTime.Now;
                    labelStatus.Text = poll.status;
                    Application.DoEvents();
                    while (true)
                    {
                        DateTime CurrCheck = DateTime.Now;
                        if (CurrCheck.AddMilliseconds(-100) > LastCheck)
                        {
                            poll = PollTTSRequestStatus(Req);
                            labelStatus.Text = poll.status;
                            Application.DoEvents();
                            if (poll.status == "complete_success" | poll.status == "complete_failure" | poll.status == "attempt_failed" | poll.status == "dead")
                            {

                                break;
                            }
                            LastCheck = DateTime.Now;

                        }
                    }
                    stpw.Stop();
                    Console.WriteLine(stpw.Elapsed);
                    switch (poll.status)
                    {
                        case "complete_success":
                            var bytes = FakeYouCSharp.StreamTTSAudioClip(FakeYouCSharp.m_cdn + poll.maybe_public_bucket_wav_audio_path);
                            currentTTSDataWav = bytes;
                            buttonPlay.Enabled = true;
                            break;
                    }

                }
            });
        }  

        static (double[] audio, int sampleRate) ReadWav(string filePath)
        {
            var afr = new NAudio.Wave.AudioFileReader(filePath);
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample / 8);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => (double)x));



            return (audio.ToArray(), sampleRate);
        }


        static (double[] audio, int sampleRate) ReadWav(byte[] bytes)
        {
            File.WriteAllBytes("tempwav.wav", bytes);

            var afr = new NAudio.Wave.AudioFileReader("tempwav.wav");
            int sampleRate = afr.WaveFormat.SampleRate;
            int sampleCount = (int)(afr.Length / afr.WaveFormat.BitsPerSample / 8);
            int channelCount = afr.WaveFormat.Channels;
            var audio = new List<double>(sampleCount);
            var buffer = new float[sampleRate * channelCount];
            int samplesRead = 0;
            while ((samplesRead = afr.Read(buffer, 0, buffer.Length)) > 0)
                audio.AddRange(buffer.Take(samplesRead).Select(x => (double)x));


            afr.Close();
            return (audio.ToArray(), sampleRate);
        }



        private void groupBox3_Enter(object sender, EventArgs e)
        {

        }

        private void button2_Click_1(object sender, EventArgs e)
        {
            if (File.Exists(Application.StartupPath + "\\fileDONE.wav"))
            {
                SoundPlayer player = new SoundPlayer(Application.StartupPath + "\\FileDONE.wav");
                player.PlaySync();
            }
        }

        private void checkBoxRemStatic_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void groupBox2_Enter(object sender, EventArgs e)
        {

        }
    }
}
