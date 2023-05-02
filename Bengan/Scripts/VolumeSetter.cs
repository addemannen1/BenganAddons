using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VolumeSetter : MonoBehaviour
{
    private enum VolumeType {
        Music = 1,
        Effect = 2
    }
    [SerializeField] private VolumeType volume;
    private void Start() {
        VolumeManager.Instance.SetVolume(volume == VolumeType.Music,GetComponent<AudioSource>());
    }
}
