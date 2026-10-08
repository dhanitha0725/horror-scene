## INTE 42312: Virtual & Augmented Reality

Group Project (50% Continuous Assessment)

Title: Design and Development of an End-to-End Virtual Reality Application

Deadline: 10th October 2026.

## 1. Background & Objective

Augmented Reality adds digital content on top of the real world. Virtual Reality is different: it builds the entire environment the user sees, so every part of it is the responsibility of your team, not just the parts you add. In groups of 3–4, you will design, build, test, and present a complete, working VR application in Unity: one coherent experience, from start to finish, that a first-time user can understand and complete through the interactions with the VR application and without being shown how by an external person. It should not feel like a set of separate features placed next to each other. The finished project should work like a minimum viable product: complete, usable version of a real product, not just a prototype.

Hardware. You will build and test everything inside the Unity Editor, using the XR Interaction Toolkit's XR Device Simulator, a tool that lets you try out a VR experience on a normal computer without wearing a headset. A physical headset is not required for this project; however, demonstrating on one is an advantage, since it shows the experience as it is meant to be felt. Designing for hardware you cannot personally test is a real skill used in the VR industry: where a claim cannot be verified directly (for example, how comfortable something feels in a headset), support it with published guidance from a reliable source, and state plainly in your presentation what you were, and were not, able to verify.

This assessment carries no written report. Your working project and your presentation are the only record of what you did. This means your presentation must explain your design decisions, how you built the project, and what your testing found, in full detail, not as a brief summary.

## 2. Learning Outcomes

- Apply the VR theory covered in this module (presence; the vergence-accommodation conflict, a mismatch between where the eyes focus and where they point, which is a common cause of eye strain in VR; cybersickness; the comfort/locomotion spectrum; and more) to real design decisions in your project, rather than simply repeating the theory back.

- Build a working VR application in Unity, correctly using its standard VR toolchain to handle tracking, movement, and hands-on interaction with objects in the world.

- Design a single, unified experience with a clear beginning, middle, and end, complete with user interfaces.

- Plan and manage a group project within a fixed scope and timeline, and demonstrate your individual contribution to the finished result.

- Present and defend your design decisions honestly, including a clear account of what could not be verified without physical hardware, and how that limitation was addressed.

## 3. Group Work

You will complete this project in the same groups formed at the start of the module. The mark is shared equally across the whole team, so it is in every member's own interest to contribute consistently throughout in order to earn the maximum marks available; see Section 8 and Section 10 for how this is shown and assessed.


## 4. What “End-to-End” Actually Means

Your application must work as one complete product, not a demonstration of separate, unconnected features.

A first-time user should be able to understand what it’s asking of them, complete it, and come away with the impression of having used something built with real intent and care: comparable, in polish and consistency, to a product designed for release.

A submission that meets this standard typically has the following characteristics:

- A clear starting point. Within the first few seconds, the user understands what is being asked of them and why; nothing is left to guesswork.

- One narrative, not a list of features. Each element of the build is there because it belongs, in a sequence that makes sense, not because it was ticked off a list.

- A real conclusion. Something is resolved: a goal is achieved, a scenario ends, or the user is informed of their outcome, or obtains a complete experience.

- A consistent environment. The visuals, lighting, and sound convey a single design intent, rather than appearing assembled from whichever assets were most readily available.

- Nothing included just to tick a box. Each feature should earn its place in the overall experience.

## 5. Technical Requirements

Built entirely in Unity, using its standard VR toolchain (the XR Interaction Toolkit, targeting OpenXR), your project can run across different headsets. Beyond that starting point, every experience must actually deliver on the following, each one fitted to your specific concept rather than bolted on to tick a box:

- Movement: a way for the player to get around that you can justify, positioned deliberately on the comfort/freedom spectrum, with comfort defaults switched on (for example, a seated experience where appropriate, or a comfort aid such as a narrowed field of view or stepped turning where continuous turning is used).

- Handling: believable, hands-on interaction with objects in the world, picking things up, activating them, and manipulating them. Build this the way Unity's toolchain intends, driven by named actions rather than a button or key hard-coded into your logic, so it keeps working across different controllers.

- Physics: objects behave with plausible weight, with realistic collisions and manipulation, and nothing floating or clipping through the environment.

- A fully immersive world: a spatial interface (menus and information placed within the 3D world itself, not a flat screen overlay), plus a considered, justified approach to audio (spatial/binaural sound is the default expectation in VR, the reverse of the mono convention used for AR in this module).

- Assets: as many 3D assets as your experience needs, all appropriately licensed and credited per Section 12. Building your own is an advantage.

- An advanced feature of your own choosing, beyond the above, that meaningfully extends the experience and stays fully testable within the XR Device Simulator. Justify it in your presentation.

- A clear beginning and a clear end, as defined in Section 4.

Out of scope. Multiplayer or networked VR, and anything the Simulator cannot test. Specify clearly, any further Out of scope items are present in the project.


## 6. Justify the Two Bridge Decisions

Each group must present, and be able to defend live at the demonstration:

- 1. The tracking-origin choice: floor-referenced (the virtual floor matches the real floor) or device-referenced (position set relative to where the headset started), and how well it fits the seated, standing, or room-scale design you adopted.

- 2. The rationale for VR over AR, using the decision framework established in this module: explain what fully replacing the real world gives you here that adding to it would not. Where the concept is a training or simulation scenario, support this with D.I.C.E. (Dangerous / Impossible / Counterproductive / Expensive), a checklist for when simulating a situation makes more sense than experiencing the real thing.

## 7. Testing Requirement

- Simulator walkthrough testing. A member other than the builder tests every required feature, and the experience as a whole, from start to finish.

- An honest account of what Simulator testing cannot establish: how comfortable the experience actually feels, real-world scale and reach, and presence (the sense of really being there). For each of these, state the published guidance or safe default you used instead. This is presented as part of the session described below (Section 8, item 13).

- Exported-build check. Since input is action-based (Section 5), add a keyboard-and-mouse control scheme as a fallback binding on the same actions. Where no physical headset is available, use this fallback to run the exported Windows build outside the Editor, since the Simulator is not present there, then confirm it launches and responds correctly, and present these results as part of the session described below (Section 8, item 11).

## 8. Presentation & Demonstration Structure

Each group delivers a single continuous session: presentation and live demonstration together. As there is no written report, this session is the complete record of the work undertaken and must cover it in full detail. The individual contribution declaration and the project timeline MUST be presented first, and clearly.

| # | Segment | Reference description |
| --- | --- | --- |
| 1 | Title & team introduction | Project title, module reference, member names |
| 2 | Individual contribution declaration | Each member states the specific part of the build for which they are responsible, and may be questioned on it |
| 3 | Project management & timeline | The group's own project schedule, presented as a Gantt chart or equivalent timeline: what was planned, what actually occurred, and where and why deviations arose |
| 4 | Problem statement | The specific real-world problem addressed, the affected users, and its significance |
| 5 | Vision statement | One or two sentences describing what success looks like for the intended user |
| 6 | Objectives (up to 3) | The concrete, measurable goals into which the vision is broken down |
| 7 | Theoretical grounding | The module concepts underpinning the design |
| 8 | Proposed solution / concept overview | The experience itself, and how it realises the stated vision |
| 9 | Design rationale | The locomotion, comfort, interaction, audio, and spatial UI decisions, each justified, together with the two bridge decisions from Section 6 |
| 10 | System architecture & technical approach | How your XR Origin and Interactor/Interactable structure work; where your advanced feature (Section 5) fits in; and your AI-tool disclosure (Section 12) |
| 11 | Testing & evaluation | What was tested, how, and the resulting findings, including the exported-build check from Section 7 where applicable |


| 12 | Challenges faced & solutions implemented | What went wrong, and precisely how it was resolved |
| --- | --- | --- |
| 13 | Limitations & constraints | An honest account of what the Simulator could not establish (Section 7) |
|   | 14 Conclusion & reflection | Whether the stated problem was solved, what was learned, and each member's individual reflection |
|   | 15 References | Displayed on screen; external sources, licensed assets, and published guidance credited, not narrated |
| 16 | Live demonstration | The experience is run live, start to finish. Each member then operates and explains the part they built, against the Section 5 criteria, and answers panel questions on it |

Groups may divide their time across segments 1–15 as they judge best; this is itself part of what is being assessed, so it is a guide to plan against, not a rigid script to follow segment by segment. The presentation (items 1–15) should run for a minimum of 20 and a maximum of 30 minutes in total. The live demonstration (item 16), including questions from the panel as each member operates and explains their part, should not exceed 10 minutes. A recorded demonstration will NOT be accepted unless a technical justification is provided in advance.

## 9. Submission Requirements

| Deliverable | Requirement |
| --- | --- |
| Source code | Complete Unity project (Git repository link or zipped project folder; Library directory excluded) |
| Build | An exported Windows build that runs independently of the Unity Editor; where no headset is available, verified using the keyboard-and-mouse fallback described in Section 7 |
| Presentation & demonstration | See Section 8. There is no written report; the presentation constitutes the complete assessment record. |

## 10. Assessment Criteria: 50%

| Category | Marks | Covers |
| --- | --- | --- |
| Technical Implementation & Physical Realism | 13 | Tracking setup; convincing movement and handling; locomotion and comfort defaults; the advanced feature; asset licensing |
| End-to-End Completeness, Cohesion, Features & Immersion | 10 | A clear start and end; every required feature integrated into one experience and contributing to it; visuals, audio, and space consistent enough to sustain immersion. Assessed on how well it all comes together, regardless of how novel the concept is. |
| Originality & Creativity | 7 | How far the concept and its execution go beyond the obvious, whether drawn from Section 11 or independently proposed. Assessed on inventiveness, regardless of how polished or complete the build is. |
| Design Rationale & Application of Theory | 10 | The rigour with which locomotion, comfort, and interaction choices are justified against the module's theory, together with the D.I.C.E./AR-vs-VR case |
| Presentation, Planning & Communication | 3 | The clarity and completeness of the full Section 8 sequence, including the declaration and timeline. In the absence of a written report, this is the sole record of the design rationale, testing, and reflection, assessed on completeness rather than delivery alone, together with how coherently the group's work is presented as a single whole |
| Project Demonstration | 3 | A working, feature-complete demonstration that runs unaided from start to finish; each member able to operate and explain their part |


| Individual Contribution | 4 | Accuracy of the declaration; accountability demonstrated live; quality of individual reflection |
| --- | --- | --- |
| Total | 50% |   |

## 11. Suggested Project Ideas

The following are offered as a starting point, not as an exhaustive list. Whether a group selects one of these or proposes its own, the conclusion required under Section 4 should arise as the natural outcome of an actual task within it.

| # | Idea | Premise |
| --- | --- | --- |
| 1 | Virtual Museum | Guided walkthrough of a themed gallery; examine exhibits, trigger narration, conclude at a finale exhibit |
| 2 | Underwater Exploration | Navigate a reef, catalogue marine species against live or simulated survey data, complete a dive log |
| 3 | Fire Safety & Evacuation Drill | Identify hazards and the correct extinguisher or exit within a burning building, evacuate within a time limit |
| 4 | Historical Walkthrough | Recreate a specific historical site or event; interact with period objects to reveal its narrative |
| 5 | First-Aid / CPR Rehearsal | Step-sequenced emergency-response practice, non-clinical in nature, concluding in a completed procedure |
| 6 | Space Station Systems Repair | Diagnose and resolve a malfunctioning system through a multi-step interaction sequence |
| 7 | Architectural Walkthrough | Visualise an unbuilt space; place and adjust furniture and fittings against a stated design brief |
| 8 | Escape Room | Sequenced puzzles across connected rooms, concluding in a real escape or completion state |

## 12. Academic Integrity & Group Work Policy

You may use openly licensed assets, platforms, plugins, and libraries, provided they are credited appropriately. Credit every third-party asset, plugin, or platform in two places: inside the build itself (for example, an in- experience credits screen) and in the presentation's References segment (Section 8, item 15). The design and the implementation must be your own group's original work.

Disclosing AI use. Where generative AI tools were used at any stage of the work, this must be disclosed in full: which tools were used, and for which parts of the work. Include this disclosure, along with how much each tool was used, in the system architecture segment of the presentation (Section 8, item 10). Disclosure is not itself the safeguard: marks are awarded for demonstrated understanding, so where a component was AI-assisted, the responsible member must be able to explain and defend the reasoning behind it live, to the same standard expected if they had written it unaided, tested through the existing live-defence points in Sections 6 and 8 (items 2 and 16).
