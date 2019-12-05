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
using IBM.Watson.LanguageTranslator.V3;
using IBM.Watson.LanguageTranslator.V3.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace IBM.Watsson.Examples.SurvivalShooter
{
    public class LanguageTranslator : MonoBehaviour
    {
        [Space(10)]
        [Tooltip("The service URL (optional). This defaults to \"https://gateway.watsonplatform.net/language-translator/api\"")]
        [SerializeField]
        private string serviceUrl;
        [Tooltip("The IAM apikey.")]
        [SerializeField]
        private string apikey;

        [Header("Parameters")]
        [SerializeField]
        private string translationModel;

        [Header("References")]
        [SerializeField]
        [Tooltip("Text field to display the results of translation.")]
        private Text LanguageTranslationResultsField;

        private LanguageTranslatorService languageTranslatorService;

        private void Start()
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

            languageTranslatorService = new LanguageTranslatorService("2019-11-12", authenticator);

            if (!string.IsNullOrEmpty(serviceUrl))
            {
                languageTranslatorService.SetServiceUrl(serviceUrl);
            }
        }

        public void Translate(string text)
        {
            if(!string.IsNullOrEmpty(text))
            {
                languageTranslatorService.Translate(
                    callback: (DetailedResponse<TranslationResult> response, IBMError error) =>
                    {
                        if (response.Result.Translations != null && response.Result.Translations.Count > 0)
                        {
                            LanguageTranslationResultsField.text = string.Format("translation: {0}", response.Result.Translations[0]._Translation);
                        }
                    }, 
                    text: new List<string> { text }, 
                    modelId: translationModel
                    );
            }
        }
    }
}