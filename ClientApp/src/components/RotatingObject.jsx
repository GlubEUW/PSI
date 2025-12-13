import { useRef } from "react";
import { useLoader, useFrame } from "@react-three/fiber";
import { GLTFLoader } from "three/examples/jsm/loaders/GLTFLoader";

const RotatingObject = ({ scale = [1.5, 1.5, 1.5], position = [0, -1.2, 0] }) => {
  const ref = useRef();
  const gltf = useLoader(GLTFLoader, "/Arcade.glb");

  useFrame(() => {
    if(ref.current) ref.current.rotation.y += 0.01;
  });

  return (
   <>
      <primitive
         ref={ref}
         object={gltf.scene}
         scale={scale}
         position={position}
      />
   </>
  );
};

export default RotatingObject;
