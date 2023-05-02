using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Bengan;
using UnityEngine.UI;

public class VolumeManager : BenganVolumeMono<VolumeManager> {
    [Header("Pre-set audio sources")]
    [SerializeField] private List<AudioSource> effect_sources = new List<AudioSource>();
    [SerializeField] private List<AudioSource> music_sources = new List<AudioSource>();
    [Header("References")]
    [SerializeField] private Slider master_slider;
    [SerializeField] private Slider music_slider;
    [SerializeField] private Slider effects_slider;
    private float master_volume;
    private float music_volume;
    private float effect_volume;
    protected override void Awake() {
        base.Awake();
        //read from file
        SessionDataHandler.Initialize();
        SessionDataHandler.OpenFile("Settings");
        master_slider.value = SessionDataHandler.GetVarFloat("MasterVolume",1f);
        master_volume = master_slider.value;
        music_slider.value = SessionDataHandler.GetVarFloat("MusicVolume",1f);
        music_volume = music_slider.value;
        effects_slider.value = SessionDataHandler.GetVarFloat("EffectVolume",1f);
        effect_volume = effects_slider.value;
        
        //set audio
        float effects_volumes = master_volume * effect_volume;
        float music_volumes = master_volume * music_volume;
        foreach (var source in effect_sources) { source.volume = effects_volumes; }
        foreach (var source in music_sources) { source.volume = music_volumes; }
    }
    public void SetVolume(bool is_music_volume, AudioSource aud) {
        if (is_music_volume) {
            music_sources.Add(aud);
            aud.volume = master_volume * music_volume;
        }
        else {
            effect_sources.Add(aud);
            aud.volume = effect_volume * music_volume;
        }
    }
    public void SetMasterVolume() {
        SessionDataHandler.Initialize();
        SessionDataHandler.OpenFile("Settings");
        SessionDataHandler.SetVarFloat("MasterVolume",master_slider.value);
        master_volume = master_slider.value;
        foreach (var source in effect_sources) { source.volume = master_volume*effect_volume; }
        foreach (var source in music_sources) { source.volume = master_volume*music_volume; }
    }
    public void SetMusicVolume() {
        SessionDataHandler.Initialize();
        SessionDataHandler.OpenFile("Settings");
        SessionDataHandler.SetVarFloat("MusicVolume",music_slider.value);
        music_volume = music_slider.value;
        foreach (var source in music_sources) { source.volume = music_volume*master_volume; }
    }
    public void SetEffectsVolume() {
        SessionDataHandler.Initialize();
        SessionDataHandler.OpenFile("Settings");
        SessionDataHandler.SetVarFloat("EffectVolume",effects_slider.value);
        effect_volume = effects_slider.value;
        foreach (var source in effect_sources) { source.volume = effect_volume*master_volume; }
    }
}
