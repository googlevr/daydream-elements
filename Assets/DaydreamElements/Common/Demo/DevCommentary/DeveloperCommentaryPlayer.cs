// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the License);
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using UnityEngine;
using System.Collections;

/// Singleton commentary player.  Will only play a
/// single commentary clip at a time.  If one is playing
/// and another is triggered, it will queue it up, fade
/// out the active clip, and play the queued clup.
public class DeveloperCommentaryPlayer : MonoBehaviour {


  private static DeveloperCommentaryPlayer instance;

  [SerializeField]
  private AudioSource source;

  private AudioClip currentClip;
  private AudioClip queuedClip;

  private float currentVolume = 1;
  private float targetVolume = 1;

  public static DeveloperCommentaryPlayer Instance {
    get{
      return instance;
    }
  }

  void Awake() {
    if (instance != null) {
      Debug.LogError("Cannot have multiple instances of DeveloperCommentaryPlayer.");
      Destroy(this);
      return;
    }
    instance = this;
  }

  void Start() {
    Object.DontDestroyOnLoad(gameObject);
  }

  public void PlayAudio(AudioClip clip, float volume = 1) {
    queuedClip = clip;
    targetVolume = volume;

    if(!source.isPlaying) {
      currentClip = queuedClip;
      source.clip = currentClip;
      source.volume = targetVolume;
      currentVolume = targetVolume;
      source.Play();
    }

  }

  void Update() {
    if(queuedClip != currentClip) {
      currentVolume = currentVolume - Time.deltaTime;
      if(currentVolume <= 0) {
        currentVolume = 0;
        source.Stop();

        currentClip = queuedClip;
        source.clip = currentClip;
        source.volume = targetVolume;
        source.Play();
        currentVolume = targetVolume;
      } else {
        source.volume = currentVolume;
      }
    }
  }

}
