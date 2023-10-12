# On chain City builder 

This is a multiplayer cooperative but also competitive city on chain builder game.
Many players can together build buildings, collect productions and upgrade the city.
But at some point the players can decide if they want to upgrade the evil goblin character or the good human character. 
Every player has its own energy system which refills over time and can be used to perform actions in the game. Like this the game can be played for free but the player can also buy sol to refill the energy faster and it incentivises to play with multiple players together

## Features

- gum session keys for auto approval of transactions
- energy system
- web socket connection for fast game updates  
- Unity Client 
- Refill energy for sol 

Note that neither the program nor session keys are audited. Use at your own risk. 

You can try a live version of the game deployed on devnet here: 
https://solplay.de/humansandgoblins/

How to run this example:
Follow the installation here: https://www.anchor-lang.com/docs/installation
sh -c "$(curl -sSfL https://release.solana.com/v1.16.20/install)"

Anchor program
1. Install the [Anchor CLI](https://project-serum.github.io/anchor/getting-started/installation.html)
2. `cd city-builder` `cd program` to enter the program directory
3. Run `anchor build` to build the program
4. Run `anchor deploy` to deploy the program
5. Copy the program id from the terminal into the lib.rs, anchor.toml and within the unity project in the LumberjackService and if you use js in the anchor.ts file
6. Build and deploy again

Unity client 
1. Install [Unity](https://unity.com/)
2. Open the lumberjack scene
3. Hit play
4. After doing changes to the anchor program make sure to regenerate the C# client: https://solanacookbook.com/gaming/porting-anchor-to-unity.html#generating-the-client
Its done like this (after you have build the program): 
cd program 
dotnet tool install Solana.Unity.Anchor.Tool <- run once
dotnet anchorgen -i target/idl/lumberjack.json -o target/idl/Lumberjack.cs

then copy the c# code into the unity project

To connect to local host from Unity add these links on the wallet holder game object: 
http://localhost:8899
ws://localhost:8900

Here are two videos explaining the energy logic and session keys: 
Session keys:
https://www.youtube.com/watch?v=oKvWZoybv7Y&t=17s&ab_channel=Solana
Energy system: 
https://www.youtube.com/watch?v=YYQtRCXJBgs&t=4s&ab_channel=Solana

# Energy System  

Many casual games in traditional gaming use energy systems. This is how you can build it on chain.
Recommended to start with the Solana cookbook [Hello world example]([https://unity.com/](https://solanacookbook.com/gaming/hello-world.html#getting-started-with-your-first-solana-game)).  

## Anchor program 

Here we will build a program which refills energy over time which the player can then use to perform actions in the game. 
In our example it will be a lumber jack which chops trees. Every tree will reward on wood and cost one energy. 

### Creating the player account

First the player needs to create an account which saves the state of our player. Notice the last_login time which will save the current unix time stamp of the player he interacts with the program. 
Like this we will be able to calculate how much energy the player has at a certain point in time.  
We also have a value for wood which will store the wood the lumber jack chucks in the game.

```rust

pub fn init_player(ctx: Context<InitPlayer>) -> Result<()> {
    ctx.accounts.player.energy = MAX_ENERGY;
    ctx.accounts.player.last_login = Clock::get()?.unix_timestamp;
    Ok(())
}

...

#[derive(Accounts)]
pub struct InitPlayer <'info> {
    #[account( 
        init, 
        payer = signer,
        space = 1000,
        seeds = [b"player".as_ref(), signer.key().as_ref()],
        bump,
    )]
    pub player: Account<'info, PlayerData>,
    #[account(mut)]
    pub signer: Signer<'info>,
    pub system_program: Program<'info, System>,
}

#[account]
pub struct PlayerData {
    pub name: String,
    pub level: u8,
    pub xp: u64,
    pub wood: u64,
    pub energy: u64,
    pub last_login: i64
}
```

### Calculating the energy

The interesting part happens in the update_energy function. We check how much time has passed and calculate the energy that the player will have at the given time. 
The same thing we will also do in the client. So we basically lazily update the energy instead of polling it all the time. 
The is a common technic in game development. 

```rust

const TIME_TO_REFILL_ENERGY: i64 = 60;
const MAX_ENERGY: u64 = 10;

pub fn update_energy(ctx: &mut ChopTree) -> Result<()> {
    let mut time_passed: i64 = &Clock::get()?.unix_timestamp - &ctx.player.last_login;
    let mut time_spent: i64 = 0;
    while time_passed > TIME_TO_REFILL_ENERGY {
        ctx.player.energy = ctx.player.energy + 1;
        time_passed -= TIME_TO_REFILL_ENERGY;
        time_spent += TIME_TO_REFILL_ENERGY;
        if ctx.player.energy == MAX_ENERGY {
            break;
        }
    }

    if ctx.player.energy >= MAX_ENERGY {
        ctx.player.last_login = Clock::get()?.unix_timestamp;
    } else {
        ctx.player.last_login += time_spent;
    }

    Ok(())
}
```

### Production times 

The city consists of a 10 by 10 grid of tiles. Each tile can be upgraded to a certain level.

When a new production is started a time stamp is saved in that tile. When the player interacts with the tile again the program will check if the production time has passed and if so it will give the player the production.
The production time is calculated by the level of the tile. 

```rust
self.data[x as usize][y as usize].building_type = building_type;
self.data[x as usize][y as usize].building_start_collect_time =
    Clock::get()?.unix_timestamp;
```

When the production is collected the time is updated and the resources are payed out to the global resources counter: 

```rust
if (Clock::get()?.unix_timestamp
    - self.data[x as usize][y as usize].building_start_collect_time)
    < 60
{
    return err!(GameErrorCode::ProductionNotReadyYet);
}

self.data[x as usize][y as usize].building_start_collect_time =
    Clock::get()?.unix_timestamp;

let collect_amount =
    calculate_building_collection(self.data[x as usize][y as usize].building_level);

if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_SAWMILL {
    self.wood += collect_amount;
} else if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_MINE {
    self.stone += collect_amount;
}    
```

### Session keys

Session keys is an optional component. What it does is creating a local key pair which is toped up with some sol which can be used to autoapprove transactions. The session token is only allowed on certain functions of the program and has an expiry of 23 hours. Then the player will get the sol back and can create a new session.  

With this you can now build any energy based game and even if someone builds a bot for the game the most he can do is play optimally, which maybe even easier to achieve when playing normally depending on the logic of your game.

This game becomes even better when combined with the Token example from Solana Cookbook and you actually drop some spl token to the players. 

### Game Actions 

### Game Actions 

The problem with subscribing to the board account is that we only get the new state of the account, but not the events which changed the account. 
There are some solutions to this. Anchor has an event system so you can use emit event and listen to it in the clients as can be seen here. 
https://docs.rs/anchor-lang/latest/anchor_lang/macro.emit.html

In this city builder the events are implemented manually in a GameActions vector. 
All events are added in the vector and the clients can see which events have already been played by their actions id.

```rust 
#[account]
pub struct GameActionHistory {
    id_counter: u64,
    game_actions: Vec<GameAction>,
}
```

When the player interacts with the program the events are added to the vector.
This is how the events are added to the vector:

```rust
game_actions.game_actions[game_actions.action_index as usize] = game_action;
game_actions.action_index = (game_actions.action_index + 1) % 30;
```

Whenever there is a new action added the client will get an update via the websocket connection.
This is how they are handled in the Unity C# client:

```C#
foreach (GameAction gameAction in gameActionHistory.GameActions)
{
    if (!alreadyPrerformedGameActions.ContainsKey(gameAction.ActionId))
    {
        // Ship shot
        if (gameAction.ActionType == 0)
        {
            MessageRouter.RaiseMessage(new BuildingCollectedAction()
            {
                BuildingType etc... 
                ... 
            });
        }
        // handle other events ...

        alreadyPerformedGameActions.Add(gameAction.ActionId, gameAction);
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