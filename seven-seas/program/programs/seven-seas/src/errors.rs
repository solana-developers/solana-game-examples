use anchor_lang::error_code;

#[error_code]
pub enum TinyAdventureError {
    TileOutOfBounds,
    BoardIsFull,
    PlayerAlreadyExists,
    TriedToMovePlayerThatWasNotOnTheBoard,
    WrongDirectionInput,
}
