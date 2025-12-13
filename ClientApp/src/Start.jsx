import { useNavigate } from "react-router-dom";
import { PostGuestUser, PostUserLogin, PostUserRegistration } from "./api/user";
import { Suspense, useState } from "react";
import { Canvas } from "@react-three/fiber";
import { OrbitControls } from "@react-three/drei";
import { EffectComposer, Pixelation } from "@react-three/postprocessing";
import RetroButton from "./components/RetroButton";
import RotatingObject from "./components/RotatingObject";
import LoginPopup from "./components/LoginPopup";
import RegisterPopup from "./components/RegisterPopup";
import GuestPopup from "./components/GuestPopup";

function Start() {
   const navigate = useNavigate();

   const [isGuestOpen, setIsGuestOpen] = useState(false);
   const [isLoginOpen, setIsLoginOpen] = useState(false);
   const [isRegisterOpen, setIsRegisterOpen] = useState(false);

   const openGuest = () => setIsGuestOpen(true);
	const openLogin = () => setIsLoginOpen(true);
   const openRegister = () => setIsRegisterOpen(true);
	const closePopup = () => {
      setIsGuestOpen(false);
      setIsLoginOpen(false);
      setIsRegisterOpen(false);
   };

   const handleGuestLogin = async (username) => {
      if (!username.trim()) {
         alert("Please enter a username.");
         return;
      }

      try {
         const response = await PostGuestUser(username);

         if (!response.ok) {
            const errorText = await response.text();
            alert("Login failed: " + errorText);
            return;
         }

         const token = await response.text();
         localStorage.setItem("userToken", token);
         navigate("/home");

      } catch (error) {
         console.error("Error during guest login: ", error);
         alert("Something went wrong. Check console.");
      }
   };

   const handleLogin = async (username, password) => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }
      if(!password.trim()) {
         alert("Please enter a password.");
         return;
      }

      try {
         const response = await PostUserLogin(username, password);

         if(!response.ok) {
            const errorText = await response.text();
            alert("Login failed: " + errorText);
            return;
         }
         const token = await response.text();
         localStorage.setItem("userToken", token);
         console.log("Login successfull");
         navigate("/home");

      } catch (error) {
         console.error("Error during user registration: ", error);
         alert("Something went wrong. Check console.");
      }
   };

   const handleRegister = async (username, password) => {
      if(!username.trim()) {
         alert("Please enter a username.");
         return;
      }
      if(!password.trim()) {
         alert("Please enter a password.");
         return;
      }

      try {
         const response = await PostUserRegistration(username, password);

         if(!response.ok) {
            const errorText = await response.text();
            console.error("Registration failed: ", errorText);
            return;
         }

         alert("Registration successfull!");
         handleLogin(username, password);

      } catch (error) {
         console.error("Error during user registration: ", error);
      }
   };

   return (
      <div style={{ display: "flex", width: "100vw", height: "100vh" }}> 
         <div
            style={{
               width: "35%",
               height: "100%",
               padding: "10px",
               color: "#0095ffff",
               display: "flex",
               flexDirection: "column",
               gap: "20px",
               justifyContent: "center",
            }}
         >
            <h1>Arcade Mayhem</h1>
            <>
               <RetroButton onClick={openGuest} bg="#2aaac4ff">Continue as Guest</RetroButton>
               <GuestPopup handleSubmit={handleGuestLogin} isPopupOpen={isGuestOpen} closePopup={closePopup}/>
            </>
            <>
               <RetroButton onClick={openLogin} bg="#ff9d00ff">Login</RetroButton>
               <LoginPopup handleSubmit={handleLogin} isPopupOpen={isLoginOpen} closePopup={closePopup}/>
            </>
            <>
               <RetroButton onClick={openRegister} bg="#44b17bff">Register</RetroButton>
               <RegisterPopup handleSubmit={handleRegister} isPopupOpen={isRegisterOpen} closePopup={closePopup}/>
            </>
         </div>

         <div style={{ width: "65%", height: "100%" }}>
            <Canvas dpr={0.5} camera={{ position: [0, 2, 5], fov: 40 }}>
               <Suspense fallback={null}>
                  <RotatingObject />
                  <EffectComposer>
                     <Pixelation granularity={5} />
                  </EffectComposer>
               </Suspense>
               <OrbitControls enableZoom={false} enablePan={false} />
            </Canvas>
         </div>
      </div>
   );
}

export default Start;