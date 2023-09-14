# seven-seas

## Description

Seven seas is a full on chain multiplayer pvp game with pirates. 

Try out the game at https://solplay.de/sevenseas/index.html deployed to solana devnet. 

There is a 7 Day bootcamp around the seven seas game: 
https://www.youtube.com/watch?v=0P8JeL3TURU&t=8s&ab_channel=Solana

## Features

The game is build so that it covers lots of features an on chain game dev would like to have as references. 

- Unity Client 
- Js client 
- Using NFTs as avatar 
- Using NFTs as ship
- Fast sending of transactions by caching blockhash 
- Saving data in PDAs bound to NFTS to be able to upgrade ships and their stats 
- Winning SPL tokens by destroying other ships and collecting chests 
- SPL tokens can be used to upgrade ships 
- Cannon and Rum SPL tokens are used to increase damage and health of ships 
- Using zero copy accounts to save a grid of the game board where the players move on 
- Using Clockwork threads to automatically move ships in their current look direction every two seconds 
- Auto approval of transactions using an in game wallet 
- Scanning Solana Pay QR codes to let Chuthulu shoot at pirate ships. Like this the audience can join in on the fun 
- Game Actions array which acts as an event system so the game clients can show animations

## How to play

Visit: https://solplay.de/sevenseas/index.html and connect to any DevNet solana wallet 
Pick an NFT and spawn it as a Pirate Ship. 

## How does it work

The game consists of multiple parts. 

- Anchor program for the game logic 
- Unity Client 
- Js Client

### Anchor program

The anchor program is the main part of the game. It is written in rust and uses the anchor framework.

lib.rs is the entry point of the program. 
All Instruction can be found in the instructions folder. 
The state of the game is saved in the game.rs file. 

Lets look at one instruction as an example. 
Move is the instruction which moves a ship on the game board. 

The move instruction.rs is calling the game.rs state function move_in_direction.
The board consists of a 2d array of tiles which is configurable in size.

```rust 
#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct GameDataAccount {
    board: [[Tile; BOARD_SIZE_X]; BOARD_SIZE_Y],
    action_id: u64,
}
```

Every tile saves which ship is on it at the moment and when a ship moved from one tile to another the data in the tile is just changed to the new ship and the old tile is set to empty. 
Its also possible to have one PDA per tile. The advantage of having all in one big account means from the client you just need to subscribe to one account via websocket which decreases the RPC credits and there will not be any race conditions. A disadvantage of having all ships in one account is that when there are many players on the board the board account could become write locked. 


### Clockwork thread (wind)

Clockwork is an open source automation tool which lets you call instructions on your program on certain triggers. For example at certain times or account changes. 
In Seven Seas it is used to simulate wind. The thread is started in the start_thread.rs file and will move all ships every 2 seconds in their current move direction. (Notice that at the moment clockwork threads on devnet are working very slowly, this will hopefully be solved soon)


### Solana Pay QR Code (Cthulhu) 

The game has a little QR code in the upper left corner can be scanned with any mobile wallet to do a solana pay transaction request. This transaction lets the Cthulhu monster in the top left to shoot at the closest ship. Like this when the game is played on a big screen the audience can also join in on the fun. 

To learn about Solana Pay transaction requests check out day 7 of the Solana Bootcamp: 
https://github.com/solana-developers/pirate-bootcamp/tree/main/quest-7

### Game Actions 

The problem with subscribing to the board account is that we only get the new state of the account, but not the events which changed the account. 
There are some solutions to this. Anchor has an event system so you can use emmit event and listen to it in the clients as can be seen here. 
https://docs.rs/anchor-lang/latest/anchor_lang/macro.emit.html

In Seven seas the events are implemented manually in a GameActions vector. 
All events are added in the vector and the clients can see which events have already been played by their actions id.

```rust 
#[account]
pub struct GameActionHistory {
    id_counter: u64,
    game_actions: Vec<GameAction>,
}
```

and this is how they are handled in the Unity C# client:

```C#
foreach (GameAction gameAction in gameActionHistory.GameActions)
{
    if (!alreadyPrerformedGameActions.ContainsKey(gameAction.ActionId))
    {
        // Ship shot
        if (gameAction.ActionType == 0)
        {
            MessageRouter.RaiseMessage(new ShipShotMessage()
            {
                ShipOwner = gameAction.Player,
                Damage = gameAction.Damage
            });
        }
        // handle other events ...

        alreadyPrerformedGameActions.Add(gameAction.ActionId, gameAction);
    }
}
```

Another option to implement events would be the Anchor Events. These write the event into the program logs, so they are saved in the ledger which makes it cheap to save them. Then in the java script client its possible to subscribe to these events.  
The problem is there are no filters for these events, but it can also be helpful. 

```rust 
use anchor_lang::prelude::*;

// handler function inside #[program]
pub fn initialize(_ctx: Context<Initialize>) -> Result<()> {
    emit!(MyEvent {
        data: 5,
        label: [1,2,3,4,5],
    });
    Ok(())
}

#[event]
pub struct MyEvent {
    pub data: u64,
    pub label: [u8; 5],
}
```

```js
let [event, slot] = await new Promise((resolve, _reject) => {
  listener = program.addEventListener("MyEvent", (event, slot) => {
    resolve([event, slot]);
  });
  program.rpc.initialize();
});
```

### Auto approval 

Seven seas uses a keypair which is saved in the browser to auto approve game transactions. 
For this it uses the Unity SDK ingame wallet. 
When a new game is started the in game wallet needs to be filled up with some sol and can be withdrawn any time. 

Here is a small presentation about different ways on how to do auto approve transactions: 
https://docs.google.com/presentation/d/1r8GDvFMBGki-hzgky4k3ZZ9evPREV2CR/edit?usp=sharing&ouid=113473212828066666910&rtpof=true&sd=true


### Randomness 

The spawning of ships and the Cthuluh damage is defined by pseudo randomness: 

```Rust
pub struct XorShift64 {
    a: u64,
}

impl XorShift64 {
    pub fn next(&mut self) -> u64 {
        let mut x = self.a;
        x ^= x << 13;
        x ^= x >> 7;
        x ^= x << 17;
        self.a = x;
        x
    }
}
```

This is an easy way to do randomness, but not really a secure one. People could bundle it with other instructions which fail when they do not like the outcome for example. 
Here is a presentation about randomness on chain: 
https://docs.google.com/presentation/d/14lKMsx8s5RIabXw4_ft4LpiZkNkpgPcD/edit#slide=id.g249a3952b21_0_0









