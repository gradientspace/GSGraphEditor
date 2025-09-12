rmdir /S /Q mujoco
curl -o mujoco.zip -L https://github.com/google-deepmind/mujoco/releases/download/3.3.5/mujoco-3.3.5-windows-x86_64.zip
mkdir mujoco
tar -xf mujoco.zip -C mujoco
del /F /Q mujoco.zip