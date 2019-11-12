# watson-survival-shooter
Watson enabled survival shooter game

## Prerequisites
1) You'll need an IBM Cloud account and instances set up for `Speech to Text`, `Assistant` and `Language Translator`.
2) You will need to create an assistant with dialog skill and intents set up for `air-support` and `teleport`.

## Usage
1) Open the scene `_Complete-Game` in `Assets/_Complete-Game`.
1) In the inspector for the `Watson` GameObject in the Hierarchy add `apikey` and `service url` for `Speech Recognition`, `Intent Classification` and `Language Translator` scripts. You can also add a `Recognize Model` for speech recognition and a `Translation Model` for language translation.
1) Play the scene and speak into the microphone commands to trigger `air-support` and `teleport` intents

## Note
* Scripts for the game can be found in `Scripts/Watson`