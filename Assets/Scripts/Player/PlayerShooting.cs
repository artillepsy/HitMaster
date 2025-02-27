﻿using System.Collections;
using Core;
using Navigation;
using UnityEngine;

namespace Player
{
    public class PlayerShooting : MonoBehaviour
    {
        [SerializeField] private Transform body;
        [SerializeField] private Transform fireSource;
        [SerializeField] private float angularSpeedSec = 400f;
        [SerializeField] private float planeDistance = 10f;
        [SerializeField] private AudioClip shootAudioClip;
        
        private AudioSource _audioSrc;
        private Coroutine _aimCO;        
        private bool _shootingEnabled = false;
        private Camera _cam;
        
        private void Start()
        {
            _cam = Camera.main;
            _audioSrc = GetComponent<AudioSource>();
            PlayerMovement.OnPlayerRotate.AddListener(() => _shootingEnabled = true);
            PlayerMovement.OnPlayerStartMove.AddListener(() =>
            {
                var endPoint = transform.position + transform.forward * 10f;
                if (_aimCO != null) StopCoroutine(_aimCO);
                _aimCO = StartCoroutine(AimCO(endPoint));
            });
            Waypoint.OnClearWaypoint.AddListener(() => _shootingEnabled = false);
        }

        private void Update()
        {
            fireSource.forward = body.forward;
            
            if (!_shootingEnabled) return;
            
            if (Input.touchCount == 0) return;
            if (Input.touches[0].phase != TouchPhase.Began) return;

            var ray = _cam.ScreenPointToRay(Input.touches[0].position);
            var plane = new Plane(-_cam.transform.forward, 
                transform.position + _cam.transform.forward * planeDistance);
            
            if (!plane.Raycast(ray, out var distance)) return;
            
            var endPoint = ray.GetPoint(distance);
            SpawnBullet(endPoint);
            if (_aimCO != null) StopCoroutine(_aimCO);
            _aimCO = StartCoroutine(AimCO(endPoint));
        }

        private IEnumerator AimCO(Vector3 endPoint)
        {
            var rotation = Quaternion.LookRotation(endPoint - fireSource.position);
            rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
            
            while (Quaternion.Angle(body.transform.rotation, rotation) > 0.5f)
            {
                body.transform.rotation = Quaternion.RotateTowards(body.transform.rotation,
                    rotation,
                    Time.deltaTime * angularSpeedSec);
                yield return null;
            }
            body.transform.rotation = rotation;
            _aimCO = null;
        }
        
        private void SpawnBullet(Vector3 endPoint)
        {
            var pos = fireSource.transform.position;
            var rotation = Quaternion.LookRotation(endPoint - fireSource.position);
            ObjectPool.Inst.Spawn(Literals.Tags.Bullet, pos, rotation);
            _audioSrc.PlayOneShot(shootAudioClip);
        }
    }
}