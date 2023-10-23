use anchor_lang::prelude::*;

#[account]
pub struct GameData {
    pub total_wood_collected: u64,
}

impl GameData {
    pub fn on_tree_chopped(&mut self, amount_chopped: u64) -> Result<()> {
        match self.total_wood_collected.checked_add(amount_chopped) {
            Some(v) => {
                self.total_wood_collected = v;
                msg!("Total wood chopped: {}", v);
            }
            None => {
                msg!("The ever tree is completly chopped!");
            }
        };

        Ok(())
    }
}
