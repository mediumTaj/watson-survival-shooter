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

using IBM.Cloud.SDK;
using IBM.Cloud.SDK.Authentication.Iam;
using IBM.Cloud.SDK.Utilities;
using IBM.Watson.Assistant.V2;
using IBM.Watson.Assistant.V2.Model;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace IBM.Watsson.Examples.SurvivalShooter
{
    public class IntentClassification : MonoBehaviour
    {
        #region PLEASE SET THESE VARIABLES IN THE INSPECTOR
        [Space(10)]
        [Tooltip("The service URL (optional). This defaults to \"https://gateway.watsonplatform.net/assistant/api\"")]
        [SerializeField]
        private string serviceUrl;
        [Tooltip("The IAM apikey.")]
        [SerializeField]
        private string apikey;
        [Tooltip("The Assistant ID.")]
        [SerializeField]
        private string assistantId;

        [Header("References")]
        [SerializeField]
        [Tooltip("Text field to display the results of classification.")]
        private Text ClassificationResultsField;
        [SerializeField]
        [Tooltip("WatsonEnabled script reference.")]
        private WatsonEnabled watsonEnabled ;
        #endregion

        private AssistantService assistantService;
        private string sessionId;

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

            assistantService = new AssistantService("2019-11-12", authenticator);

            //  Create a session
            CreateSession();
        }

        private void CreateSession()
        {
            assistantService.CreateSession(OnCreateSession, assistantId);
        }

        private void OnCreateSession(DetailedResponse<SessionResponse> response, IBMError error)
        {
            sessionId = response.Result.SessionId;
        }

        public string Classify(string text)
        {
            if (string.IsNullOrEmpty(assistantId))
            {
                throw new ArgumentNullException("assistantId is required");
            }

            if (string.IsNullOrEmpty(sessionId))
            {
                throw new ArgumentNullException("sessionId is required");
            }

            if(string.IsNullOrEmpty(text))
            {
                return null;
            }

            MessageInput input = new MessageInput()
            {
                Text = text
            };

            string classification = default(string);
            double? confidence = default(double?);
            assistantService.Message(
                callback: (DetailedResponse<MessageResponse> response, IBMError error) =>
                {
                    if (response.Result.Output.Intents != null && response.Result.Output.Intents.Count > 0)
                    {
                        classification = response.Result.Output.Intents[0].Intent;
                        confidence = response.Result.Output.Intents[0].Confidence;

                        ClassificationResultsField.text = string.Format("classification: {0}, confidence: {1:0.00}", classification, confidence);

                        if(classification == "air-support")
                        {
                            EventManager.Instance.SendEvent("OnAirSupportRequest");
                        }

                        if(classification == "teleport")
                        {
                            EventManager.Instance.SendEvent("OnTeleportRequest");
                        }
                    }
                },
                assistantId: assistantId,
                sessionId: sessionId,
                input: input
                );

            return classification;
        }
    }
}
