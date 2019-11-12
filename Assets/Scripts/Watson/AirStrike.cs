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

using UnityEngine;
using IBM.Cloud.SDK;

public class AirStrike : MonoBehaviour
{
    //  Overlap sphere radius.
    private int blastRadius = 15;
    //  Maximum amount of damage at the detonation point.
    private int maxDamage = 100;
    //  Enemy layer mask.
    private int shootableMask;
    //  Has the Airstrike been detonated?
    private bool isDetonated = false;

    void OnEnable()
    {
        shootableMask = LayerMask.GetMask("Shootable");
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!isDetonated)
        {
            AirstrikeDamage(gameObject.transform.position);
        }
    }

    public void AirstrikeDamage(Vector3 detonationPoint)
    {
        //  Dispatch event to flash
        EventManager.Instance.SendEvent("OnAirstrikeCollide");

        //  Get colliders in overlap sphere.
        Collider[] hitColliders = Physics.OverlapSphere(detonationPoint, blastRadius, shootableMask);

        //  Iterate through colliders
        foreach (Collider hitCollider in hitColliders)
        {
            //  get enemy health component.
            CompleteProject.EnemyHealth enemyHealth = hitCollider.gameObject.GetComponentInChildren<CompleteProject.EnemyHealth>();
            if (enemyHealth != null)
            {
                if (Vector3.Distance(detonationPoint, hitCollider.transform.position) < blastRadius)
                {
                    //  find distance.
                    float distance = Vector3.Distance(detonationPoint, hitCollider.transform.position);

                    //  find damage.
                    int damage = -Mathf.RoundToInt(Mathf.Pow(distance / blastRadius, 2)) + maxDamage;

                    //  raycast to find the point where the blast hits.
                    RaycastHit damageHit;
                    Physics.Raycast(detonationPoint, (hitCollider.transform.position - detonationPoint), out damageHit);
                    Vector3 hitPoint = damageHit.point;

                    //Debug.DrawRay(detonationPoint, hitCollider.transform.position - detonationPoint, Color.red, Mathf.Infinity);

                    Log.Debug("AirStrike", "damage: {0}, hitPoint: {1}, distance: {2}", damage, hitPoint, distance);

                    //  deal damage. 
                    enemyHealth.TakeDamage(damage, hitPoint);
                }
            }
        }

        isDetonated = true;

        Destroy(gameObject);
    }
}