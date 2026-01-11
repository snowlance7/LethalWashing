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
            startFallingPosition = transform.position;

            targetFloorPosition = GetGrenadeThrowDestination(ejectFrom, ejectDistance);

            hasHitGround = false;
            fallTime = 0f;
        }

        public Vector3 GetGrenadeThrowDestination(Transform ejectPoint, float _throwDistance = 10f)
        {
            Vector3 position = base.transform.position;
            grenadeThrowRay = new Ray(ejectPoint.position, ejectPoint.forward);

            if (!Physics.Raycast(grenadeThrowRay, out grenadeHit, _throwDistance, stunGrenadeMask, QueryTriggerInteraction.Ignore))
            {
                position = grenadeThrowRay.GetPoint(_throwDistance - 2f);
            }
            else
            {
                position = grenadeThrowRay.GetPoint(grenadeHit.distance - 0.05f);
            }

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
        }

        public override void FallWithCurve()
        {
            float magnitude = (startFallingPosition - targetFloorPosition).magnitude;

            Quaternion targetRotation = Quaternion.Euler(itemProperties.restingRotation.x, base.transform.eulerAngles.y, itemProperties.restingRotation.z);
            base.transform.rotation = Quaternion.Lerp(base.transform.rotation, targetRotation, 14f * Time.deltaTime / magnitude);

            base.transform.localPosition = Vector3.Lerp(startFallingPosition, targetFloorPosition, grenadeFallCurve.Evaluate(fallTime));

            if (magnitude > 5f)
            {
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurveNoBounce.Evaluate(fallTime)
                );
            }
            else
            {
                base.transform.localPosition = Vector3.Lerp(
                    new Vector3(base.transform.localPosition.x, startFallingPosition.y, base.transform.localPosition.z),
                    new Vector3(base.transform.localPosition.x, targetFloorPosition.y, base.transform.localPosition.z),
                    grenadeVerticalFallCurve.Evaluate(fallTime)
                );
            }

            fallTime += Mathf.Abs(Time.deltaTime * 12f / magnitude);
        }

        public override void OnHitGround()
        {
            base.OnHitGround();
            RoundManager.PlayRandomClip(audioSource, coinDropSFXs.ToArray());
        }

        [ClientRpc]
        public void SetScrapValueClientRpc(int setValueTo)
        {
            SetScrapValue(setValueTo);
        }
    }
}
