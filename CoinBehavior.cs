using BepInEx.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    internal class CoinBehavior : PhysicsProp
    {
        private static ManualLogSource logger = LoggerInstance;

#pragma warning disable 0649
        public AnimationCurve grenadeFallCurve = null!;
        public AnimationCurve grenadeVerticalFallCurve = null!;
        public AnimationCurve grenadeVerticalFallCurveNoBounce = null!;
        public AudioSource CoinAudio = null!;
        public List<AudioClip> CoinDropSFXs = [];
#pragma warning restore 0649

        private Ray grenadeThrowRay;
        private RaycastHit grenadeHit;
        private int stunGrenadeMask = 268437761;

        public override void Start()
        {
            base.Start();
            if (WashingMachine.Instance == null) { return; }
            EjectFromWashingMachine(WashingMachine.Instance.CoinSpawn);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination(playerHeldBy.gameplayCamera.transform));
        }

        public void EjectFromWashingMachine(Transform ejectFrom)
        {
            logIfDebug("ejectFrom.position: " + ejectFrom.position);
            logIfDebug("ejectFrom.transform.position: " + ejectFrom.transform.position);
            logIfDebug("transform.position" + transform.position);
            startFallingPosition = transform.position;
            logIfDebug("Start: " + startFallingPosition);

            targetFloorPosition = GetGrenadeThrowDestination(ejectFrom, configEjectDistance.Value); // -21.4333 -2.6356 -25.0231
            logIfDebug("End: " + targetFloorPosition);

            hasHitGround = false;
            fallTime = 0f;
        }

        /*public Vector3 GetGrenadeThrowDestination(Transform ejectPoint)
        {
            Vector3 position = base.transform.position;
            grenadeThrowRay = new Ray(ejectPoint.position, ejectPoint.forward);
            position = ((!Physics.Raycast(grenadeThrowRay, out grenadeHit, 12f, stunGrenadeMask, QueryTriggerInteraction.Ignore)) ? grenadeThrowRay.GetPoint(10f) : grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f));
            grenadeThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                position = grenadeHit.point + Vector3.up * 0.05f;
            }
            else
            {
                position = grenadeThrowRay.GetPoint(30f);
            }

            position += new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
            return position;
        }*/

        public Vector3 GetGrenadeThrowDestination(Transform ejectPoint, float _throwDistance = 10f)
        {
            Vector3 position = base.transform.position;
            grenadeThrowRay = new Ray(ejectPoint.position, ejectPoint.forward);

            // Adjusted throw distance
            if (!Physics.Raycast(grenadeThrowRay, out grenadeHit, _throwDistance, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                position = grenadeThrowRay.GetPoint(_throwDistance - 2f); // Adjust target point
            }
            else
            {
                position = grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f);
            }

            // Second raycast downward to find the ground
            grenadeThrowRay = new Ray(position, Vector3.down);
            if (Physics.Raycast(grenadeThrowRay, out grenadeHit, 30f, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                position = grenadeHit.point + Vector3.up * 0.05f;
            }
            else
            {
                position = grenadeThrowRay.GetPoint(30f);
            }

            // Add randomness
            position += new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));

            return position;
        }


        public override void FallWithCurve()
        {
            // Log initial state
            logIfDebug($"cFallWithCurve called. Start Position: {startFallingPosition}, Target Position: {targetFloorPosition}, Initial cfallTime: {fallTime}");

            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            logIfDebug($"Calculated magnitude: {magnitude}");

            // Log rotation interpolation
            Quaternion targetRotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetRotation, 14f * Time.deltaTime / magnitude);
            logIfDebug($"Updated rotation to: {base.transform.rotation.eulerAngles}");

            // Log position interpolation for primary fall
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));
            logIfDebug($"Updated primary fall position to: {base.transform.localPosition}");

            // Conditional logging for vertical fall curve
            if (magnitude > 5f)
            {
                logIfDebug("Magnitude > 5, using grenadeVerticalFallCurveNoBounce.");
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurveNoBounce.Evaluate(fallTime)
                );
            }
            else
            {
                logIfDebug("Magnitude <= 5, using grenadeVerticalFallCurve.");
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurve.Evaluate(fallTime)
                );
            }

            // Log updated position and fallTime
            logIfDebug($"Updated local position after vertical fall: {base.transform.localPosition}");

            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
            logIfDebug($"Updated cfallTime to: {fallTime}");
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            logIfDebug("Hit ground");
            RoundManager.PlayRandomClip(CoinAudio, CoinDropSFXs.ToArray());
        }
    }
}
