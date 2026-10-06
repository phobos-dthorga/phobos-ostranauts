# Machine work sounds

Since Framework 0.119.0, Manufacturing 0.57.0, Shipbreaker 0.84.0 and Agriculture
0.64.0, Phobos machines hum while they work: a steady motor-and-pump loop that
fades in when a machine starts working and out when it stops. Not yet heard in
the game; the owner's listening checks are below.

## What you hear

- **Only real work.** A machine sounds while it is doing work with power reaching
  it. It goes quiet when it pauses, finishes, runs out of feed or power, waits for
  a cool room, or is unloaded. Turning the panel on or off never plays anything.
- **Each machine its own.** Every machine plays one of eight loops at its own
  pitch, so two different machines do not sound alike. Big machines run a little
  lower, small ones a little higher.
- **As loud as the game's own.** The sounds use the same bus, distance falloff and
  range as the game's air scrubber, and each loop is levelled to the scrubber's
  loudness. They fade with distance, follow the game's own volume settings and
  muffle in thin air like the game's machines.
- **The nearest few.** Only the four nearest working machines are heard at once,
  so a room of machinery does not drone together.

| Mod | Machines that sound |
| --- | --- |
| Manufacturing | V4, LC-3, SA-3, Copperhead-3, EC-4, CR-4, X2, K2, AX-2, L2, Corker-2 and RM-1 while working; the A2 only while gas flows into the room |
| Shipbreaker | D4, R4, T2 and C2 while working; the F6 while it heats a batch |
| Agriculture | Firstlight-4 while its lamps are lit, Hearth-2, Groundwork B2 and the W2 while working |

Silent on purpose: the G4 grabber and ML-2 laser (they work outside the hull,
where no sound carries), the Ward-3 bed and Vigil-2 monitor (a bed in use for a
patient's whole stay would hum beside a sleeping patient; the monitor has no
moving parts), and everything passive: stores, tanks, silos, lines, belts, bins,
the P1 manifold and the navigation boards.

## Settings

In `BepInEx/config/phobosgekko.ostranauts.framework.cfg`, section `Audio`:

- `MachineSoundVolume` (0 to 1, default 1): 1 is the game's own appliance level;
  0 mutes machine sounds. Separate from `CompletionCueVolume`.
- `MachineSoundVoices` (0 to 12, default 4): how many working machines are heard
  at once, nearest first.

The game's own effects and master volume apply on top. At start-up the log says
what loudness the sounds were levelled to.

## Owner checks

1. Start a V4 batch: the loop fades in; Pause it and the loop fades out.
2. Walk away: it fades with distance like the scrubber; a docked ship's machines
   stay quiet beyond the same range.
3. Five machines working in one room: only the nearest four are heard, without a
   phased drone.
4. Set `MachineSoundVolume` to 0: silence, and every machine keeps working.
5. Pause the game, time-skip and reload: no burst of queued sound, and a machine
   is only heard once it is working again.

The sounds are ElevenLabs-generated (attribution: ElevenLabs, elevenlabs.io); see
the [audio record](../assets/phobos-audio/washer-motor-pump-v1/README.md).
