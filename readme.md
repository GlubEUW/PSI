# PSI Team Project  

## How to set up web app in localhost
### 1. Have the following dependencies installed:
   - .NET SDK (Version 9)
   - Node.Js (Installing Node.js will automatically install npm)
   - npm
   
### 2. Start backend API.  
   From the **/PSI** directory:  
   ```
   cd Api 
   dotnet run
   ```

### 3. Start frontend.  
   From the **PSI/** directory:  
   **If you don't have dependencies installed, run** 
   ```
   cd ClientApp
   npm install
   ```
   
   ```
   cd ClientApp
   npm run dev    
   ```
   <br>

## Funtionality list:
- **Turn-based games**  
Games will be made on the basis of each player having to wait for their turn to make a move in the game. Each match will have a set time limit (for each player separately) to limit the amount of time users spend waiting. For matchmaking, there will be a fixed time limit, while the time limit for custom game sessions can be customized by the session creator. 

- **Matchmaking**  
The user has the ability to join a matchmaking queue which will search for other people who are currently waiting in a matchmaking queue for the same game. Once enough players are in a matchmaking queue for the same game, they are matched together. In theory, different users will be able to get matched with random people from the internet. A penalty for leaving a tournament session early could also be implemented, punishing players who leave the match instead of finishing it.
*Examples: League of Legends, Counter Strike, Valorant, etc.*  

- **Tournament bracket**  
Users are able to start or join a tournament session which will have the players compete with one another for placements in a tournament style bracket.  
*Example: 8 players play against each other for one time each, the player with the most wins at the end gets first place, the player with the second most wins gets second and so on.*  

- **Link based invite system**  
Each non-matchmaking game session will have it's own unique web URL which can be copied and sent to other users in order for them to join the same game session.

- **Guest user**  
The user isn't required to make an account in order to play games, a username is enough.

- **Account management**  
The user can set up an account which will store and track their game data (match history, win/loss ratio in certain games, longest win/lose streak). They will also be able to view their stats, change their name or password.

- **Game data**  
Game data will be collected and stored in a database for each game (games played, play time, games won/lost/winrate). Tournament sessions will require more data to be stored (participants, number of rounds played, play time, tournament placements, game data for each separate round). Data about each turn played during a match will be saved to be able to watch match replay.

- **Leaderboard**  
A leaderboard ranking users with the most wins (or highest winrate percentage), updated live.

- **Spectating**  
Users have the ability to spectate other people's matches when sent an invite link.

- **Chat system**  
Users can chat with one another while in the same game session. An option to disable chat might be implemented later on.
