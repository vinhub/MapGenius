#!/bin/bash
echo "Resetting PlayerPrefs:"
plutil -p ~/Library/Preferences/unity.RedmondLabs.MapGenius.plist
defaults write ~/Library/Preferences/unity.RedmondLabs.MapGenius HideInstructionsAtStart -int 0
plutil -p ~/Library/Preferences/unity.RedmondLabs.MapGenius.plist
echo "Done."