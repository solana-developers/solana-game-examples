# Battle Coins (WIP)

Battle Coins is a simple game built using the Anchor framework. In this game, players can set up their own accounts, eliminate enemies to earn tokens, and recover health by burning tokens. A designated "admin" creates a token for the game, which functions as the in-game currency.

## Token Creation

The "admin" can create a new token mint with metadata by invoking the `create_mint` instruction. This instruction takes the URI, name, and symbol of the token as arguments, uses a PDA (Program Derived Address) as the mint authority, and creates the token metadata account for the mint.

## Creating the Player Account

To participate in the game, a player needs to create an account which stores their health. The `init_player` instruction initializes the player account with `MAX_HEALTH`.

## Eliminating Enemies

When a player decides to eliminate an enemy, they invoke the `kill_enemy` instruction. This instruction generates a random damage value using xorshift and deducts that amount from the player's health. The player is also rewarded with 1 token minted to their token account.

## Healing

To heal themselves, players can call the `heal` instruction which sets their health back to `MAX_HEALTH`. The healing process requires the player to burn one token from their token account.
