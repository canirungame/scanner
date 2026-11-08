# Can I Run Game - Scanner

A tiny Windows program that reads your PC's hardware and sends it to [canirungame.com](https://canirungame.com), so the site can tell you whether a game will run on your machine.

I made it open source because a program that reads your hardware should be something you can check before you run it.

## What it reads

- CPU model
- Graphics card(s) and their VRAM
- RAM
- Disk type and free space
- Windows version
- Screen resolution

That's the full list. It doesn't read your files, browser data, passwords, installed programs or anything about you personally. If you want to check, everything it collects is in `Scanner.cs`, and the upload happens in `MainForm.cs`.

## How it works

1. On the site you get a 6 character pairing code.
2. You run the scanner and type the code in.
3. The scanner sends the hardware info to the site, and the site matches it to your browser.

The code expires after a few minutes and works once. You don't need an account.