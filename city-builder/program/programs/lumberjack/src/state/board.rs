use anchor_lang::prelude::*;

use crate::GameErrorCode;
use crate::BOARD_SIZE_X;
use crate::BOARD_SIZE_Y;

const BUILDING_TYPE_TREE: u8 = 0;
const BUILDING_TYPE_EMPTY: u8 = 1;
const BUILDING_TYPE_SAWMILL: u8 = 2;
const BUILDING_TYPE_MINE: u8 = 3;
const BUILDING_TYPE_GOOD: u8 = 4;
const BUILDING_TYPE_EVIL: u8 = 5;

const BUILDING_START_HEALTH: i64 = 9000;

const ACTION_TYPE_CHOP: u8 = 0;
const ACTION_TYPE_BUILD: u8 = 1;
const ACTION_TYPE_UPGRADE: u8 = 2;
const ACTION_TYPE_COLLECT: u8 = 3;
const ACTION_TYPE_FIGHT: u8 = 4;
const ACTION_RESET_GAME: u8 = 5;

const GOOD_POSITION_X: usize = 5;
const GOOD_POSITION_Y: usize = 4;

const EVIL_POSITION_X: usize = 4;
const EVIL_POSITION_Y: usize = 4;

impl BoardAccount {
    /*pub fn new(&mut self) -> Self {
        let mut mySelf = Self {
            data: [[TileData::default(); BOARD_SIZE_X]; BOARD_SIZE_Y],
            action_id: 0,
            wood: 0,
            stone: 0,
            damm_level: 0,
            initialized: true,
        };

        mySelf.data[4][4].building_type = BUILDING_TYPE_EVIL;
        mySelf.data[5][4].building_type = BUILDING_TYPE_GOOD;

        mySelf
    }*/

    pub fn Restart(&mut self, game_actions: &mut GameActionHistory, player: Pubkey) -> Result<()> {
        self.data = [[TileData::default(); BOARD_SIZE_X]; BOARD_SIZE_Y];
        self.action_id = 0;
        self.wood = 0;
        self.stone = 0;
        self.damm_level = 0;

        self.data[EVIL_POSITION_X][EVIL_POSITION_Y].building_type = BUILDING_TYPE_EVIL;
        self.data[EVIL_POSITION_X][EVIL_POSITION_Y].building_health = BUILDING_START_HEALTH;

        self.data[GOOD_POSITION_X][GOOD_POSITION_Y].building_type = BUILDING_TYPE_GOOD;
        self.data[GOOD_POSITION_X][GOOD_POSITION_Y].building_health = BUILDING_START_HEALTH;
        self.initialized = true;
        self.evil_won = false;
        self.good_won = false;

        game_actions.id_counter = 0;
        game_actions.action_index = 0;
        game_actions.game_actions = [GameAction::default(); 30];

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_RESET_GAME,
            x: 0,
            y: 0,
            player: player.key(),
            avatar: player.key(),
            tile: self.data[0 as usize][0 as usize],
            amount: 0,
        };

        self.add_new_game_action(game_actions, new_game_action);

        Ok(())
    }

    pub fn chop_tree(
        &mut self,
        x: u8,
        y: u8,
        player: Pubkey,
        avatar: Pubkey,
        game_actions: &mut GameActionHistory,
    ) -> Result<()> {
        //let tile = self.board[x][y];

        if self.data[x as usize][y as usize].building_type != BUILDING_TYPE_TREE {
            return err!(GameErrorCode::TileHasNoTree);
        }

        let wood_per_chop = 5;
        self.data[x as usize][y as usize].building_type = BUILDING_TYPE_EMPTY;
        self.wood += wood_per_chop;

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_CHOP,
            x,
            y,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[x as usize][y as usize],
            amount: wood_per_chop,
        };

        self.add_new_game_action(game_actions, new_game_action);
        self.fight_good_vs_evil(game_actions, player, avatar);

        Ok(())
    }

    pub fn build(
        &mut self,
        x: u8,
        y: u8,
        building_type: u8,
        player: Pubkey,
        avatar: Pubkey,
        game_actions: &mut GameActionHistory,
    ) -> Result<()> {
        if self.data[x as usize][y as usize].building_type != BUILDING_TYPE_EMPTY {
            return err!(GameErrorCode::TileAlreadyOccupied);
        }

        let mut building_cost_stone = 0;
        let mut building_cost_wood = 0;

        if building_type == BUILDING_TYPE_SAWMILL {
            building_cost_stone = 15;
            building_cost_wood = 0;
        } else if building_type == BUILDING_TYPE_MINE {
            building_cost_stone = 0;
            building_cost_wood = 15;
        }

        if self.wood < building_cost_wood {
            return err!(GameErrorCode::NotEnoughWood);
        }
        if self.stone < building_cost_stone {
            return err!(GameErrorCode::NotEnoughStone);
        }

        self.wood -= building_cost_wood;
        self.stone -= building_cost_stone;

        self.data[x as usize][y as usize].building_type = building_type;
        self.data[x as usize][y as usize].building_start_collect_time =
            Clock::get()?.unix_timestamp;

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_BUILD,
            x,
            y,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[x as usize][y as usize],
            amount: 0,
        };
        self.add_new_game_action(game_actions, new_game_action);
        self.fight_good_vs_evil(game_actions, player, avatar);
        Ok(())
    }

    pub fn upgrade(
        &mut self,
        x: u8,
        y: u8,
        player: Pubkey,
        avatar: Pubkey,
        game_actions: &mut GameActionHistory,
    ) -> Result<()> {
        if self.data[x as usize][y as usize].building_type != BUILDING_TYPE_SAWMILL
            && self.data[x as usize][y as usize].building_type != BUILDING_TYPE_MINE
            && self.data[x as usize][y as usize].building_type != BUILDING_TYPE_GOOD
            && self.data[x as usize][y as usize].building_type != BUILDING_TYPE_EVIL
        {
            return err!(GameErrorCode::TileCantBeUpgraded);
        }

        let mut upgrade_cost_stone = 0;
        let mut upgrade_cost_wood = 0;

        if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_SAWMILL {
            upgrade_cost_stone = calculate_sawmill_stone_upgrade_cost(
                self.data[x as usize][y as usize].building_level,
            );
            upgrade_cost_wood = calculate_sawmill_wood_upgrade_cost(
                self.data[x as usize][y as usize].building_level,
            );
        } else if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_MINE {
            upgrade_cost_stone = calculate_stone_mine_stone_upgrade_cost(
                self.data[x as usize][y as usize].building_level,
            );
            upgrade_cost_wood = calculate_stone_mine_wood_upgrade_cost(
                self.data[x as usize][y as usize].building_level,
            );
        } else if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_GOOD {
            upgrade_cost_stone =
                calculate_good_stone_upgrade_cost(self.data[x as usize][y as usize].building_level);
            upgrade_cost_wood =
                calculate_good_wood_upgrade_cost(self.data[x as usize][y as usize].building_level);
        } else if self.data[x as usize][y as usize].building_type == BUILDING_TYPE_EVIL {
            upgrade_cost_stone =
                calculate_evil_stone_upgrade_cost(self.data[x as usize][y as usize].building_level);
            upgrade_cost_wood =
                calculate_evil_wood_upgrade_cost(self.data[x as usize][y as usize].building_level);
        }

        if self.wood < upgrade_cost_wood {
            return err!(GameErrorCode::NotEnoughWood);
        }
        if self.stone < upgrade_cost_stone {
            return err!(GameErrorCode::NotEnoughStone);
        }

        self.wood -= upgrade_cost_wood;
        self.stone -= upgrade_cost_stone;

        self.data[x as usize][y as usize].building_level += 1;

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_UPGRADE,
            x,
            y,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[x as usize][y as usize],
            amount: 0,
        };
        self.add_new_game_action(game_actions, new_game_action);
        self.fight_good_vs_evil(game_actions, player, avatar);
        Ok(())
    }

    pub fn collect(
        &mut self,
        x: u8,
        y: u8,
        player: Pubkey,
        avatar: Pubkey,
        game_actions: &mut GameActionHistory,
    ) -> Result<()> {
        if self.data[x as usize][y as usize].building_type != BUILDING_TYPE_SAWMILL
            && self.data[x as usize][y as usize].building_type != BUILDING_TYPE_MINE
        {
            return err!(GameErrorCode::BuildingTypeNotCollectable);
        }

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

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_COLLECT,
            x,
            y,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[x as usize][y as usize],
            amount: collect_amount,
        };
        self.add_new_game_action(game_actions, new_game_action);
        self.fight_good_vs_evil(game_actions, player, avatar);
        Ok(())
    }

    fn add_new_game_action(
        &mut self,
        game_actions: &mut GameActionHistory,
        game_action: GameAction,
    ) {
        {
            let option_add = self.action_id.checked_add(1);
            match option_add {
                Some(val) => {
                    self.action_id = val;
                }
                None => {
                    self.action_id = 0;
                }
            }
        }
        game_actions.game_actions[game_actions.action_index as usize] = game_action;
        game_actions.action_index = (game_actions.action_index + 1) % 30;
    }

    fn fight_good_vs_evil(
        &mut self,
        game_actions: &mut GameActionHistory,
        player: Pubkey,
        avatar: Pubkey,
    ) {
        if self.evil_won || self.good_won {
            return;
        }

        let good_damage: u32 = (self.data[GOOD_POSITION_X][GOOD_POSITION_Y].building_level + 1) * 2;
        let evil_damage = (self.data[EVIL_POSITION_X][EVIL_POSITION_Y].building_level + 1) * 2;

        self.data[GOOD_POSITION_X][GOOD_POSITION_Y].building_health -= evil_damage as i64;
        self.data[EVIL_POSITION_X][EVIL_POSITION_Y].building_health -= good_damage as i64;

        if self.data[EVIL_POSITION_X][EVIL_POSITION_Y].building_health <= 0 {
            self.good_won = true;
            msg!("Good wins");
        }

        if self.data[GOOD_POSITION_X][GOOD_POSITION_Y].building_health <= 0 {
            self.evil_won = true;
            msg!("Evil wins");
        }

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_FIGHT,
            x: EVIL_POSITION_X as u8,
            y: EVIL_POSITION_Y as u8,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[EVIL_POSITION_X][EVIL_POSITION_Y],
            amount: good_damage as u64,
        };

        self.add_new_game_action(game_actions, new_game_action);

        let new_game_action = GameAction {
            action_id: self.action_id,
            action_type: ACTION_TYPE_FIGHT,
            x: GOOD_POSITION_X as u8,
            y: GOOD_POSITION_Y as u8,
            player: player.key(),
            avatar: avatar.key(),
            tile: self.data[GOOD_POSITION_X][GOOD_POSITION_Y],
            amount: evil_damage as u64,
        };
        self.add_new_game_action(game_actions, new_game_action);
    }
}

// Function to calculate the wood upgrade cost of the sawmill based on the building level
fn calculate_sawmill_wood_upgrade_cost(building_level: u32) -> u64 {
    const BASE_SAWMILL_WOOD_COST: u32 = 10;
    const SAWMILL_WOOD_COST_MULTIPLIER: f64 = 1.1;

    let sawmill_wood_cost = (BASE_SAWMILL_WOOD_COST as f64
        * f64::powf(SAWMILL_WOOD_COST_MULTIPLIER, building_level as f64))
        as u64;
    sawmill_wood_cost
}

// Function to calculate the stone upgrade cost of the sawmill based on the building level
fn calculate_sawmill_stone_upgrade_cost(building_level: u32) -> u64 {
    const BASE_SAWMILL_STONE_COST: u32 = 5;
    const SAWMILL_STONE_COST_MULTIPLIER: f64 = 1.05;

    let sawmill_stone_cost = (BASE_SAWMILL_STONE_COST as f64
        * f64::powf(SAWMILL_STONE_COST_MULTIPLIER, building_level as f64))
        as u64;
    sawmill_stone_cost
}

fn calculate_stone_mine_wood_upgrade_cost(building_level: u32) -> u64 {
    const BASE_STONE_MINE_WOOD_COST: u32 = 5;
    const STONE_MINE_WOOD_COST_MULTIPLIER: f64 = 1.05;

    let stone_mine_wood_cost = (BASE_STONE_MINE_WOOD_COST as f64
        * f64::powf(STONE_MINE_WOOD_COST_MULTIPLIER, building_level as f64))
        as u64;
    stone_mine_wood_cost
}

fn calculate_stone_mine_stone_upgrade_cost(building_level: u32) -> u64 {
    const BASE_STONE_MINE_STONE_COST: u32 = 10;
    const STONE_MINE_STONE_COST_MULTIPLIER: f64 = 1.1;

    let stone_mine_stone_cost = (BASE_STONE_MINE_STONE_COST as f64
        * f64::powf(STONE_MINE_STONE_COST_MULTIPLIER, building_level as f64))
        as u64;
    stone_mine_stone_cost
}

fn calculate_evil_wood_upgrade_cost(building_level: u32) -> u64 {
    const BASE_COST: u32 = 15;
    const COST_MULTIPLIER: f64 = 1.1;

    let final_cost = (BASE_COST as f64 * f64::powf(COST_MULTIPLIER, building_level as f64)) as u64;
    final_cost
}

fn calculate_evil_stone_upgrade_cost(building_level: u32) -> u64 {
    const BASE_COST: u32 = 15;
    const COST_MULTIPLIER: f64 = 1.1;

    (BASE_COST as f64 * f64::powf(COST_MULTIPLIER, building_level as f64)) as u64
}

fn calculate_good_wood_upgrade_cost(building_level: u32) -> u64 {
    const BASE_COST: u32 = 15;
    const COST_MULTIPLIER: f64 = 1.1;

    (BASE_COST as f64 * f64::powf(COST_MULTIPLIER, building_level as f64)) as u64
}

fn calculate_good_stone_upgrade_cost(building_level: u32) -> u64 {
    const BASE_COST: u32 = 15;
    const COST_MULTIPLIER: f64 = 1.1;

    (BASE_COST as f64 * f64::powf(COST_MULTIPLIER, building_level as f64)) as u64
}

fn calculate_building_collection(building_level: u32) -> u64 {
    const BASE_COLLECTION: u32 = 5;
    const COLLECITON_MULTIPLIER: f64 = 1.1;

    (BASE_COLLECTION as f64 * f64::powf(COLLECITON_MULTIPLIER, building_level as f64)) as u64
}

#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct BoardAccount {
    pub data: [[TileData; BOARD_SIZE_X]; BOARD_SIZE_Y],
    pub action_id: u64,
    pub wood: u64,         // Global resources, let see how it goes :D
    pub stone: u64,        // Global resources, let see how it goes :D
    pub damm_level: u64,   // Global building level of the mein goal
    pub initialized: bool, // Global building level of the mein goal
    pub evil_won: bool,
    pub good_won: bool,
}

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct TileData {
    pub building_type: u8,
    pub building_level: u32,
    pub building_owner: Pubkey, // Could maybe be the avatar of the player building it? :thinking:
    pub building_start_time: i64,
    pub building_start_upgrade_time: i64,
    pub building_start_collect_time: i64,
    pub building_health: i64,
}

#[account(zero_copy(unsafe))]
#[repr(packed)]
#[derive(Default)]
pub struct GameActionHistory {
    id_counter: u64,
    action_index: u64,
    game_actions: [GameAction; 30],
}

#[zero_copy(unsafe)]
#[repr(packed)]
#[derive(Default)]
pub struct GameAction {
    action_id: u64,  // 4
    action_type: u8, // 1
    x: u8,           // 1
    y: u8,           // 1
    tile: TileData,  // 32
    player: Pubkey,  // 32
    avatar: Pubkey,  // 32
    amount: u64,     // 4
}
