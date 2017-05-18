// Copyright 2017 Google Inc. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
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
using UnityEditor;
using System.IO;

class GvrAssetPostprocessor : AssetPostprocessor {
  void OnPreprocessTexture() {
    // Reconfigure all images in Plugins/iOS as so they don't get compressed,
    // resized, or mipmapped.  Saves a bunch of import time.
    if (assetPath.Contains("Plugins/iOS")) {
      TextureImporter ti = assetImporter as TextureImporter;
      // Don't compress at all.
      ti.textureFormat = TextureImporterFormat.AutomaticTruecolor;
      // Don't rescale at all.
      ti.npotScale = TextureImporterNPOTScale.None;
      // Don't generate mipmaps.
      ti.mipmapEnabled = false;
    }
  }
}
