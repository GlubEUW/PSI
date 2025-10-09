export async function PutGuestUser(username) {
   const response = await fetch("http://localhost:5243/api/user/guest", {
      method: "PUT",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: username })
   });
   return response;
}

export async function GetGuestUser(token) {
   const response = await fetch("http://localhost:5243/api/user/guest", {
      method: "GET",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}