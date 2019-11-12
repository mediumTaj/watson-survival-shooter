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

using System;
using IBM.Cloud.SDK;
using UnityEngine;
using UnityEngine.UI;

public class WatsonEnabled : MonoBehaviour
{
    [SerializeField]
    private GameObject airStrikePrefab;

    [SerializeField]
    private Transform playerTransform;

    [SerializeField]
    private Image flashImage;

    private bool airstrikeDetonated = false;
    private Color flashColor = new Color(1f, 1f, 1f, 1f);
    private float flashSpeed = 0.01f;

    void Start()
    {
        LogSystem.InstallDefaultReactors();
    }

    void Update()
    {
        //If airstrike has gone off
        if (airstrikeDetonated)
        {
            // ... set the colour of the damageimage to the flash colour.
            flashImage.color = flashColor;
        }
        // otherwise...
        else
        {
            // ... transition the colour back to clear.
            flashImage.color = Color.Lerp(flashImage.color, Color.clear, flashSpeed * Time.deltaTime);
        }

        // reset the airstrikeDetonated flag.
        airstrikeDetonated = false;
    }

    void OnEnable()
    {
        EventManager.Instance.RegisterEventReceiver("OnAirSupportRequest", HandleAirSupportRequest);
        EventManager.Instance.RegisterEventReceiver("OnAirSupportRequestFromKeyboard", HandleAirSupportRequestFromKeyboard);
        EventManager.Instance.RegisterEventReceiver("OnAirstrikeCollide", HandleAirstrikeCollide);
        EventManager.Instance.RegisterEventReceiver("OnTeleportRequest", HandleTeleportRequest);
    }
    
    void OnDisable()
    {
        EventManager.Instance.UnregisterEventReceiver("OnAirSupportRequest", HandleAirSupportRequest);
        EventManager.Instance.UnregisterEventReceiver("OnAirSupportRequestFromKeyboard", HandleAirSupportRequestFromKeyboard);
        EventManager.Instance.UnregisterEventReceiver("OnAirstrikeCollide", HandleAirstrikeCollide);
        EventManager.Instance.UnregisterEventReceiver("OnTeleportRequest", HandleTeleportRequest);
    }

    private void HandleAirSupportRequest(object[] args)
    {
        var rotation = new Quaternion();
        rotation.eulerAngles = new Vector3(180, 0, 0);
        GameObject bomb = Instantiate(airStrikePrefab, playerTransform.localPosition + new Vector3(0f, 10f, 0f), rotation);
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.velocity = transform.TransformDirection(Vector3.down * 25);
    }

    private void HandleAirSupportRequestFromKeyboard(object[] args)
    {
        var rotation = new Quaternion();
        rotation.eulerAngles = new Vector3(180, 0, 0);
        GameObject bomb = Instantiate(airStrikePrefab, playerTransform.localPosition + new Vector3(0f, 10f, 0f), rotation);
        Rigidbody rb = bomb.GetComponent<Rigidbody>();
        rb.velocity = transform.TransformDirection(Vector3.down * 25);
    }

    private void HandleTeleportRequest(object[] args)
    {
        float x = UnityEngine.Random.Range(-17f, 17f);
        float y = 0;
        float z = UnityEngine.Random.Range(-17f, 17f);

        playerTransform.position = new Vector3(x, y, z);
    }

    private void HandleAirstrikeCollide(object[] args)
    {
        airstrikeDetonated = true;
    }
}