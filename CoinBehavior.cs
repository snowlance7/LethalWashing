using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static LethalWashing.Plugin;

namespace LethalWashing
{
    internal class CoinBehavior : PhysicsProp
    {
#pragma warning disable CS8618
        public AnimationCurve grenadeFallCurve;
        public AnimationCurve grenadeVerticalFallCurve;
        public AnimationCurve grenadeVerticalFallCurveNoBounce;
        public AudioSource audioSource;
#pragma warning restore CS8618
        public List<AudioClip> coinDropSFXs = [];

        Ray grenadeThrowRay;
        RaycastHit grenadeHit;
        const int stunGrenadeMask = 268437761;
        const float ejectDistance = 1f;

        public override void Start()
        {
            base.Start();
            if (WashingMachine.Instance == null) { return; }
            EjectFromWashingMachine(WashingMachine.Instance.coinSpawn);
        }

        public override void ItemActivate(bool used, bool buttonDown = true)
        {
            base.ItemActivate(used, buttonDown);
            playerHeldBy.DiscardHeldObject(placeObject: true, null, GetGrenadeThrowDestination(playerHeldBy.gameplayCamera.transform));
        }

        public void EjectFromWashingMachine(Transform ejectFrom)
        {
            logger.LogDebug("ejectFrom.position: " + ejectFrom.position);
            logger.LogDebug("ejectFrom.transform.position: " + ejectFrom.transform.position);
            logger.LogDebug("transform.position" + transform.position);
            startFallingPosition = transform.position;
            logger.LogDebug("Start: " + startFallingPosition);

            targetFloorPosition = GetGrenadeThrowDestination(ejectFrom, ejectDistance); // -21.4333 -2.6356 -25.0231
            logger.LogDebug("End: " + targetFloorPosition);

            hasHitGround = false;
            fallTime = 0f;
        }

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
            logger.LogDebug($"cFallWithCurve called. Start Position: {startFallingPosition}, Target Position: {targetFloorPosition}, Initial cfallTime: {fallTime}");

            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;
            logger.LogDebug($"Calculated magnitude: {magnitude}");

            // Log rotation interpolation
            Quaternion targetRotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetRotation, 14f * Time.deltaTime / magnitude);
            logger.LogDebug($"Updated rotation to: {base.transform.rotation.eulerAngles}");

            // Log position interpolation for primary fall
            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));
            logger.LogDebug($"Updated primary fall position to: {base.transform.localPosition}");

            // Conditional logging for vertical fall curve
            if (magnitude > 5f)
            {
                logger.LogDebug("Magnitude > 5, using grenadeVerticalFallCurveNoBounce.");
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurveNoBounce.Evaluate(fallTime)
                );
            }
            else
            {
                logger.LogDebug("Magnitude <= 5, using grenadeVerticalFallCurve.");
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurve.Evaluate(fallTime)
                );
            }

            // Log updated position and fallTime
            logger.LogDebug($"Updated local position after vertical fall: {base.transform.localPosition}");

            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
            logger.LogDebug($"Updated cfallTime to: {fallTime}");
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            logger.LogDebug("Hit ground");
            RoundManager.PlayRandomClip(audioSource, coinDropSFXs.ToArray());
        }

        [ClientRpc]
        public void SetScrapValueClientRpc(int setValueTo)
        {
            SetScrapValue(setValueTo);
        }
    }
}
