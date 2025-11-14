export async function PostGuestUser(username) {
   const response = await fetch("http://localhost:5243/api/user/guest", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ name: username })
   });
   return response;
}

export async function PostUserLogin(username, password) {
   const response = await fetch("http://localhost:5243/api/user/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ 
         name: username,
         password: password
      })
   });
   return response;
}

export async function PostUserRegistration(username, password) {
   const response = await fetch("http://localhost:5243/api/user/register", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ 
         name: username,
         password: password
      })
   });
   return response;
}

export async function GetUser(token) {
   const response = await fetch("http://localhost:5243/api/user/userInfo", {
      method: "GET",
      headers: {
         "Content-Type": "application/json",
         "Authorization": "Bearer " + token
      }
   });
   return response;
}