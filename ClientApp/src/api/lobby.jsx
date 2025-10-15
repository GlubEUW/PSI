export async function GetLobbyInfo(token, code) {
   const response = await fetch(`http://localhost:5243/api/lobby/${code}`, {
      method: "GET",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}

export async function JoinLobby(token, code) {
   const response = await fetch(`http://localhost:5243/api/lobby/${code}/join`, {
      method: "POST",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}

