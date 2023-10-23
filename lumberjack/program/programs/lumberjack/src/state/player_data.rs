use crate::constants::*;
use anchor_lang::prelude::*;

#[account]
pub struct PlayerData {
    pub authority: Pubkey,
    pub name: String,
    pub level: u8,
    pub xp: u64,
    pub wood: u64,
    pub energy: u64,
    pub last_login: i64,
}

impl PlayerData {
    pub fn print(&mut self) -> Result<()> {
        msg!("Name: {} Wood: {}", self.name, self.wood);
        Ok(())
    }

    pub fn update_energy(&mut self) -> Result<()> {
        let mut time_passed: i64 = Clock::get()?.unix_timestamp - self.last_login;
        let mut time_spent: i64 = 0;

        while time_passed > TIME_TO_REFILL_ENERGY {
            self.energy += 1;
            time_passed -= TIME_TO_REFILL_ENERGY;
            time_spent += TIME_TO_REFILL_ENERGY;
            if self.energy >= MAX_ENERGY {
                break;
            }
        }

        if self.energy >= MAX_ENERGY {
            self.last_login = Clock::get()?.unix_timestamp;
        } else {
            self.last_login += time_spent;
        }

        Ok(())
    }

    pub fn chop_tree(&mut self, amount: u64) -> Result<()> {
        match self.wood.checked_add(amount) {
            Some(v) => {
                self.wood = v;
            }
            None => {
                msg!("Total wood reached!");
            }
        };
        self.energy -= 1;
        Ok(())
    }
}
