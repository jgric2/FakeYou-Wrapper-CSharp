using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static FakeYou_Wrapper.Form1;

namespace FakeYou_Wrapper
{
    public class FakeYouCSharp
    {
        const string quote = "\"";
        public const string m_cdn = "https://storage.googleapis.com/vocodes-public";
        public class VoiceModel
        {
            public string model_token;
            public string tts_model_type;
            public string creator_user_token;
            public string creator_username;
            public string creator_display_name;
            public string creator_gravatar_hash;
            public string title;
            public string ietf_language_tag;
            public string ietf_primary_language_subtag;
            public bool is_front_page_featured;
            public bool is_twitch_featured;
            public string maybe_suggested_unique_bot_command;
            public string created_at;
            public string updated_at;
        }
        public class VoiceCategoryModel
        {
            public string category_token;
            public string model_type;
            public string maybe_super_category_token;
            public bool can_directly_have_models;
            public bool can_have_subcategories;
            public bool can_only_mods_apply;
            public string name;
            public string name_for_dropdown;
            public bool is_mod_approved;
            public string created_at;
            public string updated_at;
            public string maybe_suggested_unique_bot_command;
            public string deleted_at;
        }
        public class TTSPollStatus
        {
            public string job_token;
            public string status;
            public string maybe_extra_status_description;
            public int attempt_count;
            public string maybe_result_token;
            public string maybe_public_bucket_wav_audio_path;
            public string model_token;
            public string tts_model_type;
            public string title;
            public string raw_inference_text;
            public string created_at;
            public string updated_at;
        }

        #region Get
        /// <summary>
        /// Gets Voice Categories
        /// </summary>
        /// <returns></returns>
        public static List<VoiceCategoryModel> GetListOfVoiceCategories()
        {
            List<VoiceCategoryModel> retlist = new List<VoiceCategoryModel>(); ;
            WebClient client = new WebClient();
            //client.Headers.Add("Authorization:SAPI:YOUR_API_KEY_OPTIONAL");
            client.Proxy = null;
            string reply = client.DownloadString("https://api.fakeyou.com/category/list/tts");
            dynamic ReturnValue = JsonConvert.DeserializeObject(reply);
            if ((bool)ReturnValue["success"] != true)
            {
                return retlist;
            }
            var models = ReturnValue["categories"];
            foreach (var item in models)
            {
                VoiceCategoryModel vcm = new VoiceCategoryModel();
                vcm.category_token = (string)models["category_token"];
                vcm.model_type = (string)models["model_type"];
                vcm.maybe_super_category_token = (string)models["maybe_super_category_token"];
                vcm.can_directly_have_models = (bool)models["can_directly_have_models"];
                vcm.can_have_subcategories = (bool)models["can_have_subcategories"];
                vcm.can_only_mods_apply = (bool)models["can_only_mods_apply"];
                vcm.name = (string)models["name"];
                vcm.name_for_dropdown = (string)models["name_for_dropdown"];
                vcm.is_mod_approved = (bool)models["is_mod_approved"];
                vcm.created_at = (string)models["created_at"];
                vcm.updated_at = (string)models["updated_at"];
                vcm.deleted_at = (string)models["deleted_at"];
                retlist.Add(vcm);
            }
            return retlist;
        }

        /// <summary>
        /// Gets list of available voices
        /// </summary>
        /// <returns></returns>
        public static List<VoiceModel> GetListOfVoices()
        {
            //https://api.fakeyou.com/tts/list
            List<VoiceModel> retlist = new List<VoiceModel>(); ;
            WebClient client = new WebClient();
           //client.Headers.Add("Authorization:SAPI:YOUR_API_KEY_OPTIONAL");
            client.Proxy = null;
            string reply = client.DownloadString("https://api.fakeyou.com/tts/list");
            dynamic ReturnValue = JsonConvert.DeserializeObject(reply);
            if ((bool)ReturnValue["success"] != true)
            {
                return retlist;
            }

            var models = ReturnValue["models"];
            foreach (var item in models)
            {
                VoiceModel vm = new VoiceModel();
                vm.model_token = (string)item["model_token"];
                vm.tts_model_type = (string)item["tts_model_type"];
                vm.creator_user_token = (string)item["creator_user_token"];
                vm.creator_username = (string)item["creator_username"];
                vm.creator_display_name = (string)item["creator_display_name"];
                vm.creator_gravatar_hash = (string)item["creator_gravatar_hash"];
                vm.title = (string)item["title"];
                vm.ietf_language_tag = (string)item["ietf_language_tag"];
                vm.ietf_primary_language_subtag = (string)item["ietf_primary_language_subtag"];
                vm.is_front_page_featured = (bool)item["is_front_page_featured"];
                vm.is_twitch_featured = (bool)item["is_twitch_featured"];
                vm.maybe_suggested_unique_bot_command = (string)item["maybe_suggested_unique_bot_command"];
                vm.created_at = (string)item["created_at"];
                vm.updated_at = (string)item["updated_at"];
                retlist.Add(vm);
            }
            return retlist;
        }


        /// <summary>
        /// Downloads the tts audio clip to a file
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="FileName"></param>
        public static void GetTTSAudioClip(string uri, string FileName)
        {
            WebClient client = new WebClient();
            client.Proxy = null;
            client.DownloadFile(uri, FileName);
        }


        /// <summary>
        /// Streams the tts audio clip and returns the stream as a byte array, with optional boolean to play the streamed audio
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="play"></param>
        /// <returns></returns>
        public static byte[] StreamTTSAudioClip(string uri, bool play = true)
        {
            WebClient client = new WebClient();
            client.Proxy = null;
            //client.Headers.Add("Authorization:SAPI:YOUR_API_KEY_OPTIONAL");
            byte[] sound = client.DownloadData(uri);
            if (play)
            {
                PlayWavByteStream(sound);
            }
            return sound;
        }


        /// <summary>
        /// Helper funtion to play audio from a byte array
        /// </summary>
        /// <param name="data"></param>
        public static void PlayWavByteStream(byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            {
                // Construct the sound player
                SoundPlayer player = new SoundPlayer(ms);
                player.PlaySync();
            }
        }


        /// <summary>
        /// Used to hold audio files for sorting long running audio
        /// </summary>
        public class HolderTempM
        {
            public int index = 0;
            public byte[] data;
            public SoundPlayer snd;
        }

        /// <summary>
        /// Plays a list of audio files for long running audio ans stitches them together
        /// </summary>
        /// <param name="data"></param>
        public static void PlayWavByteStreamList(List<HolderTemp> data)
        {

            int current = 0;
            object lockCurrent = new object();
            var primeNumbers = new ConcurrentBag<HolderTempM>();
            List<HolderTempM> templist = new List<HolderTempM>();
            for (int i = 0; i < data.Count; i++)
            {
                HolderTempM hh = new HolderTempM();
                hh.index = i;
                hh.data = data[i].data;
                templist.Add(hh);
            }
            List<SoundPlayer> sndList = new List<SoundPlayer>();
            Parallel.For(0, templist.Count, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (ii, loopState) =>
              {
                  int thisCurrent = 0;
                  lock (lockCurrent)
                  {
                      thisCurrent = current;
                      current++;
                  }
                  MemoryStream ms = new MemoryStream(templist[thisCurrent].data);
                  templist[thisCurrent].snd = new SoundPlayer(ms);
                  primeNumbers.Add(templist[thisCurrent]);
              });

            var ll = primeNumbers.ToList();
            ll = ll.OrderBy(x=> x.index).ToList();
            foreach (var item in ll)
            {
                item.snd.PlaySync();
            }
        }


        /// <summary>
        /// Requests current status on the job token
        /// </summary>
        /// <param name="jobToken"></param>
        /// <returns></returns>
        public static TTSPollStatus PollTTSRequestStatus(string jobToken)
        {
            TTSPollStatus retval = new TTSPollStatus();
            WebClient client = new WebClient();
            //client.Headers.Add("Authorization:SAPI:YOUR_API_KEY_OPTIONAL");
            client.Proxy = null;
            string reply = client.DownloadString("https://api.fakeyou.com/tts/job/" + jobToken);
            dynamic ReturnValue = JsonConvert.DeserializeObject(reply);
            if ((bool)ReturnValue["success"] != true)
            {
                retval.status = "Failed";
                return retval;
            }
            var state = ReturnValue["state"];
            retval.job_token = (string)state["job_token"];
            retval.status = (string)state["status"];
            retval.maybe_extra_status_description = (string)state["maybe_extra_status_description"];
            retval.attempt_count = (int)state["attempt_count"];
            retval.maybe_result_token = (string)state["maybe_result_token"];
            retval.maybe_public_bucket_wav_audio_path = (string)state["maybe_public_bucket_wav_audio_path"];
            retval.model_token = (string)state["model_token"];
            retval.tts_model_type = (string)state["tts_model_type"];
            retval.title = (string)state["title"];
            retval.raw_inference_text = (string)state["raw_inference_text"];
            retval.created_at = (string)state["created_at"];
            retval.updated_at = (string)state["updated_at"];
            return retval;
        }

        #endregion

        #region Post
        /// <summary>
        /// Request a TTS job
        /// </summary>
        /// <param name="modelToken"></param>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string MakeTTSRequest(string modelToken, string text)
        {
            var request = WebRequest.Create("https://api.fakeyou.com/tts/inference");
            request.ContentType = "application/json";//; charset=utf-8";
            //request.Headers.Add("Authorization:SAPI:YOUR_API_KEY_OPTIONAL");
            request.Method = "POST";
            request.Proxy = null;
            Guid myuuid = Guid.NewGuid();
            string myuuidAsString = myuuid.ToString();
            string postData = "{" + quote + "tts_model_token" + quote + ": " + quote + modelToken + quote + ", " + quote + "uuid_idempotency_token"
                + quote + ": " + quote + myuuidAsString + quote + ", " + quote + "inference_text" + quote + ": " + quote + text + quote + "}";

            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] data = encoding.GetBytes(postData);
            request.ContentLength = data.Length;
            Stream newStream = request.GetRequestStream();
            newStream.Write(data, 0, data.Length);
            newStream.Close();

            string textB;
            var response = (HttpWebResponse)request.GetResponse();
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                textB = sr.ReadToEnd();
                dynamic ReturnValue = JsonConvert.DeserializeObject(textB);
                if ((bool)ReturnValue["success"] == true)
                {

                    return (string)ReturnValue["inference_job_token"];
                }
                else
                {
                    return "Failed";
                }
            }
        }

        #endregion

        public static byte[] GetTTSHelper(string modelToken, string text)
        {
            var Req = MakeTTSRequest(modelToken, text);
            if (Req != "Failed")
            {
                var poll = PollTTSRequestStatus(Req);
                DateTime LastCheck = DateTime.Now;
                while (true)
                {
                    DateTime CurrCheck = DateTime.Now;
                    if (CurrCheck.AddMilliseconds(-1000) > LastCheck)
                    {
                        poll = PollTTSRequestStatus(Req);
                        if (poll.status == "complete_success" | poll.status == "complete_failure" | poll.status == "attempt_failed" | poll.status == "dead")
                        {
                            break;
                        }
                        LastCheck = DateTime.Now;
                    }
                }

                switch (poll.status)
                {
                    case "complete_success":
                        var bytes = StreamTTSAudioClip(m_cdn + poll.maybe_public_bucket_wav_audio_path);
                        return bytes;
                }
                return null;
            }
            return null;
        }
    }
}
