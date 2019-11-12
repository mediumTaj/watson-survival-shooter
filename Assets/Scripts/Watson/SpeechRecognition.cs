/**
* Copyright 2019 IBM Corp. All Rights Reserved.
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*
*/
#pragma warning disable 0649

using IBM.Cloud.SDK;
using IBM.Cloud.SDK.Authentication.Iam;
using IBM.Cloud.SDK.DataTypes;
using IBM.Cloud.SDK.Utilities;
using IBM.Watson.SpeechToText.V1;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IBM.Watsson.Examples.SurvivalShooter
{
    public class SpeechRecognition : MonoBehaviour
    {
        #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
        [Space(10)]
        [Tooltip("The service URL (optional). This defaults to \"https://stream.watsonplatform.net/speech-to-text/api\"")]
        [SerializeField]
        private string serviceUrl;
        
        [Header("IAM Authentication")]
        [Tooltip("The IAM apikey.")]
        [SerializeField]
        private string apikey;

        [Header("Parameters")]
        // https://www.ibm.com/watson/developercloud/speech-to-text/api/v1/curl.html?curl#get-model
        [Tooltip("The Model to use. This defaults to en-US_BroadbandModel")]
        [SerializeField]
        private string recognizeModel;

        [Header("References")]
        [SerializeField]
        [Tooltip("Reference to the IntentClassification instance.")]
        private IntentClassification intentClassification;
        [SerializeField]
        [Tooltip("Reference to the LanguageTranslator instance.")]
        private LanguageTranslator languageTranslator;
        [SerializeField]
        [Tooltip("Text field to display the results of streaming.")]
        private Text SpeechToTextResultsField;
        #endregion


        private int recordingRoutine = 0;
        private string microphoneId = null;
        private AudioClip recording = null;
        private int recordingBufferSize = 1;
        private int recordingHZ = 22050;

        private SpeechToTextService speechToTextService;

        void Start()
        {
            LogSystem.InstallDefaultReactors();
            Runnable.Run(CreateService());
        }

        private IEnumerator CreateService()
        {
            if (string.IsNullOrEmpty(apikey))
            {
                throw new IBMException("Plesae provide IAM ApiKey for the service.");
            }

            IamAuthenticator authenticator = new IamAuthenticator(apikey: apikey);

            //  Wait for tokendata
            while (!authenticator.CanAuthenticate())
                yield return null;

            speechToTextService = new SpeechToTextService(authenticator);
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                speechToTextService.SetServiceUrl(serviceUrl);
            }
            speechToTextService.StreamMultipart = true;

            Active = true;
            StartRecording();
        }

        public bool Active
        {
            get { return speechToTextService.IsListening; }
            set
            {
                if (value && !speechToTextService.IsListening)
                {
                    speechToTextService.RecognizeModel = (string.IsNullOrEmpty(recognizeModel) ? "en-US_BroadbandModel" : recognizeModel);
                    speechToTextService.DetectSilence = true;
                    speechToTextService.EnableWordConfidence = true;
                    speechToTextService.EnableTimestamps = true;
                    speechToTextService.SilenceThreshold = 0.01f;
                    speechToTextService.MaxAlternatives = 1;
                    speechToTextService.EnableInterimResults = true;
                    speechToTextService.OnError = OnError;
                    speechToTextService.InactivityTimeout = -1;
                    speechToTextService.ProfanityFilter = false;
                    speechToTextService.SmartFormatting = true;
                    speechToTextService.SpeakerLabels = false;
                    speechToTextService.WordAlternativesThreshold = null;
                    speechToTextService.StartListening(OnRecognize, OnRecognizeSpeaker);
                }
                else if (!value && speechToTextService.IsListening)
                {
                    speechToTextService.StopListening();
                }
            }
        }

        private void StartRecording()
        {
            if (recordingRoutine == 0)
            {
                UnityObjectUtil.StartDestroyQueue();
                recordingRoutine = Runnable.Run(RecordingHandler());
            }
        }

        private void StopRecording()
        {
            if (recordingRoutine != 0)
            {
                Microphone.End(microphoneId);
                Runnable.Stop(recordingRoutine);
                recordingRoutine = 0;
            }
        }

        private void OnError(string error)
        {
            Active = false;

            Log.Debug("SpeechRecognition.OnError()", "Error! {0}", error);
        }

        private IEnumerator RecordingHandler()
        {
            Log.Debug("SpeechRecognition.RecordingHandler()", "devices: {0}", Microphone.devices);
            recording = Microphone.Start(microphoneId, true, recordingBufferSize, recordingHZ);
            yield return null;      // let recordingRoutine get set..

            if (recording == null)
            {
                StopRecording();
                yield break;
            }

            bool bFirstBlock = true;
            int midPoint = recording.samples / 2;
            float[] samples = null;

            while (recordingRoutine != 0 && recording != null)
            {
                int writePos = Microphone.GetPosition(microphoneId);
                if (writePos > recording.samples || !Microphone.IsRecording(microphoneId))
                {
                    Log.Error("SpeechRecognition.RecordingHandler()", "Microphone disconnected.");

                    StopRecording();
                    yield break;
                }

                if ((bFirstBlock && writePos >= midPoint)
                  || (!bFirstBlock && writePos < midPoint))
                {
                    // front block is recorded, make a RecordClip and pass it onto our callback.
                    samples = new float[midPoint];
                    recording.GetData(samples, bFirstBlock ? 0 : midPoint);

                    AudioData record = new AudioData();
                    record.MaxLevel = Mathf.Max(Mathf.Abs(Mathf.Min(samples)), Mathf.Max(samples));
                    record.Clip = AudioClip.Create("Recording", midPoint, recording.channels, recordingHZ, false);
                    record.Clip.SetData(samples, 0);

                    speechToTextService.OnListen(record);

                    bFirstBlock = !bFirstBlock;
                }
                else
                {
                    // calculate the number of samples remaining until we ready for a block of audio, 
                    // and wait that amount of time it will take to record.
                    int remaining = bFirstBlock ? (midPoint - writePos) : (recording.samples - writePos);
                    float timeRemaining = (float)remaining / (float)recordingHZ;

                    yield return new WaitForSeconds(timeRemaining);
                }
            }
            yield break;
        }

        private void OnRecognize(SpeechRecognitionEvent result)
        {
            if (result != null && result.results.Length > 0)
            {
                foreach (var res in result.results)
                {
                    foreach (var alt in res.alternatives)
                    {
                        string text = string.Format("{0} ({1}{2:0.00})\n", alt.transcript, res.final ? "final, " : "interim", res.final ? "confidence: " + alt.confidence : "");
                        Log.Debug("SpeechRecognition.OnRecognize()", text);
                        SpeechToTextResultsField.text = text;

                        if (res.final)
                        {
                            string classification = intentClassification.Classify(alt.transcript);
                            languageTranslator.Translate(alt.transcript);
                        }
                    }

                    if (res.keywords_result != null && res.keywords_result.keyword != null)
                    {
                        foreach (var keyword in res.keywords_result.keyword)
                        {
                            Log.Debug("SpeechRecognition.OnRecognize()", "keyword: {0}, confidence: {1}, start time: {2}, end time: {3}", keyword.normalized_text, keyword.confidence, keyword.start_time, keyword.end_time);
                        }
                    }

                    if (res.word_alternatives != null)
                    {
                        foreach (var wordAlternative in res.word_alternatives)
                        {
                            Log.Debug("SpeechRecognition.OnRecognize()", "Word alternatives found. Start time: {0} | EndTime: {1}", wordAlternative.start_time, wordAlternative.end_time);
                            foreach (var alternative in wordAlternative.alternatives)
                                Log.Debug("SpeechRecognition.OnRecognize()", "\t word: {0} | confidence: {1}", alternative.word, alternative.confidence);
                        }
                    }
                }
            }
        }

        private void OnRecognizeSpeaker(SpeakerRecognitionEvent result)
        {
            if (result != null)
            {
                foreach (SpeakerLabelsResult labelResult in result.speaker_labels)
                {
                    Log.Debug("SpeechRecognition.OnRecognizeSpeaker()", string.Format("speaker result: {0} | confidence: {3} | from: {1} | to: {2}", labelResult.speaker, labelResult.from, labelResult.to, labelResult.confidence));
                }
            }
        }
    }
}
