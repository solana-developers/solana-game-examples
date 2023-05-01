# Solana Unity Game Examples
A unity frame work build on top of the Solana Unity SDK with 4 example games. 

Follow me on Twitter for more frequent updates: @SolPlay_jonas

1) To use it in your game just include in the Unity Package manager: 
https://github.com/garbles-labs/Solana.Unity-SDK.git#v0.0.6
in your unity package manager.

2) Import this unity package (Version 0.0.13 alpha)
[solplay_0_13.unitypackage.zip](https://github.com/Woody4618/SolPlay_Unity_SDK/files/10570401/solplay_0_13.unitypackage.zip)
and then import it in Unity Assets->importPackage.

Some function may not work with the standart MainNet RPC. 
You may want to get a free RPC from quicknode or helius.xyz. 
Also currently its hard to get DevNet sol, so if the automatic airdrops fail you may need to try a few times, try different RPC in the WalletHolderService or transfer dev net sol to your Wallet or use solfaucet.com. (The publickey will be logged in the console on login) 

3) Optional: Import Glftfast for 3D NFT support
[GlFAst Installer](https://package-installer.glitch.me/v1/installer/OpenUPM/com.atteneder.gltfast?registry=https%3A%2F%2Fpackage.openupm.com&scope=com.atteneder)
Also add the precompiler flag #GLTFAST

Here are the live versions for the example games: 

All game examples work in WebGL using the Phantom browser extension and an mobile using phantom deeplinks. 
They all come with the Anchor code and Tiny Adventure one and two are also available as tutorials in the Solana 
Playground here: https://beta.solpg.io/tutorials/

- Tiny Adventure
Simple on chain game moving a character left and right: 
Live version: https://solplay.de/TinyAdventure/index.html
Tutorial: https://www.youtube.com/watch?v=_vQ3bSs3svs

- Tiny Adventure Two
Using a PDA as a chest vault and pay it out to the winner of the game. 
The chest is guarded by a password. 
Live Version: https://solplay.de/TinyAdventureTwo/index.html
Tutorial: https://www.youtube.com/watch?v=gILXyWvXu7M

- Flappy Game
Use an NFT as character for a Flappy Bird like game. Nfts are cached in the client and the loading of the Image and the meta data are seperated.
Live Version: https://solplay.de/

- Sol Hunter 
Realtime Multplayer PvP game where players collect chests and kill other players for sol token.
Uses an auto approve wallet by asking the player to deposit sol into an ingame wallet. 
Live Version: https://solplay.de/SolHunter/index.html

Here is a Video which explains the process step by step: (A bit out dated, you can now skip the step 3) 
[https://www.youtube.com/channel/UC517QSv61gMaABWIJ412_Lw/videos](https://youtu.be/mS5Fx_yzcHw)

Release notes:
0.0.13 Alpha
- Examples tiny adventure and tiny adventure two
- Bug fixes
- Removed the SolPlay fork since the unity sdk now has WebSockets as well

0.0.12 Alpha
- Source code for the Realtime Multiplayer Battle Royal game SolHunter
- Bug fixes

0.0.9 Alpha
- Faster NFt loading by seperating Json from the Image loading and using UniTasks to yield tasks in WebGL
- Socket connection now works with all RPC providers I know. Check out the SolPlaySocketService
- All folders are not within the SolPlay folder to have less clutter in the root folder 
- Tiny AdvenureTutorial Anchor example client (https://beta.solpg.io/tutorials/tiny-adventure)
- Source code of SolHunter realtime multiplayer game
- Made GLTFast dependency optional: Add GLTFAST compiler flag and install the package to use it 

If you want to participate, it's very welcome.


Packages used: 

Native WebSockets by Endel:
https://github.com/endel/NativeWebSocket

WebSocketSharp: 
https://github.com/sta/websocket-sharp

Epic Toon FX:
https://assetstore.unity.com/packages/vfx/particles/epic-toon-fx-57772

glTFast for runtime loading of 3D NFTs:
https://github.com/atteneder/glTFast

Lunar console (Get the pro version here: 
https://github.com/SpaceMadness/lunar-unity-console
Pro Version: https://assetstore.unity.com/packages/tools/gui/lunar-mobile-console-pro-43800)

Garbels unity solana sdk. Check out their awesome game as well! Vr Pokemon! 
https://github.com/garbles-dev/Solana.Unity/tree/master/src

Solanart:
https://github.com/allartprotocol/unity-solana-wallet

Tweetnacl (removed):
https://github.com/dchest/tweetnacl-js/blob/master/README.md#random-bytes-generation

Gif loading:
https://github.com/3DI70R/Unity-GifDecoder

Flappy Bird Game: 
https://github.com/diegolrs/Flappy-Bird

Unity Ui Extensions:
https://github.com/JohannesDeml/unity-ui-extensions.git

UniTask to be able to have delays in WebGL: 
https://github.com/Cysharp/UniTask/releases

Unity mainThread dispatcher to be able to call unity function from socket messages:
https://github.com/PimDeWitte/UnityMainThreadDispatcher

Anchor to C# code generation
https://github.com/garbles-labs/Solana.Unity.Anchor
https://github.com/bmresearch/Solnet.Anchor/

So far the repository is only tested in IOS mobile, Android and WebGL.

Done:
- Realtime Multiplayer Battle Royal game in the examples including Anchor code
- Login and getting Public key from phantom
- Loading and caching NFTs
- Nft meta data parsing + power level calculation
- Deeplink to minting page
- Deeplink to raydium token swap
- Transactions
- In game token and sol amount loading widget
- WebGL support 
- IOS Support 
- Android Support
- Smart contract interaction
- Token swap using Orca WhirlPools
- Minting NFTs using metaplex (Without candy machine)
- Minting NFTs (from candy machine V2)
- Socket connection so listen to account updates
- Faster NFT loading by seperating the json loading from loading the images and starting multipl at the same time with a smaller rate limit
- Two anchor example games with source code here: https://beta.solpg.io/tutorials/
- 3D NFts using GLTF fast. Needs to have a glb file in the animation url of the NFT
- InGame auto approve wallet. Check our the SolHunter example for how it works. 


Next up Todo: 

- Animated Gifs
- Minting NFTs (from candy machine V2 with white list tokens)
- Maybe Staking? 
- What else would you like?  
- Try back porting the socket solution to the Garbles SDK to be able to use StreamingRPCs
- Tiny Adventure Two example 



