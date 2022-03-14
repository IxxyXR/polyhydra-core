# Polyhydra

![Screenshot](https://pro2-bar-s3-cdn-cf1.myportfolio.com/1e3b6316-da77-4fd2-a111-e12070c11b10/2977d391-d8a0-4759-8f3b-fe112b8957b8_rwc_0x22x975x549x975.png?h=f2ff1682c51247d1bc76e926872686e2)

Polyhydra is a toolkit for the procedural generation of geometric forms in Unity. The above image is from a VR piece I made using it called "Gossamer" that is currently exhibited in the Museum of Other Realities: https://andybak.net/gossamer

* YouTube playlist: https://youtube.com/playlist?list=PL94EgLgEIJyJQh_nB-CvSKbXjNU0ojNqC
* Gallery: https://andybak.net/polyhydra

# Getting Started

This repo is a cleanup and an attempt to extract the core functionality for reuse in other apps.

Right now you are better off using the repo here instead: https://github.com/IxxyXR/polyhydra-upm

# Credits

As far as possible I'd like to licence this under the MIT licence or similar but the code has a complex heritage.

Obviously the original work by Willem Wythoff and John Conway. And also countless other mathematicians who have formed a base for, contributed to and extended the work in this area. A special shout out to George Hart who is often co-credited with Conway due to the large amount of work he did exploring and extending Conway's original operators.

The actual Wythoff code was based on https://github.com/kaonasi (which in turn is based on the work of Zvi Har’El: http://www.math.technion.ac.il/S/rl/kaleido/ Zvi Har'El has sadly passed away. I've tried to contact all potential copyright holders to see if it's OK to make use of their work as a basis for this but I've had no luck in getting a response. Please get in touch if you're an interested party)

Conway operator code and the core halfedge mesh is based on work by Will Pearson @mcneel which can be found here: https://github.com/pearswj/buckminster

Multigrids is ported from work by Wolthera van Hövell tot Westerflier for https://github.com/kde/krita - they generously agreed that my version could be MIT licenced.

Portions of grids.cs is from Antiprism and is MIT but should be attributed to Adrian Rossiter and Roger Kaufman: https://github.com/antiprism/antiprism/blob/master/COPYING

Isohedral tilings are from tactile.js https://github.com/isohedral/tactile-js Thanks to Craig Kaplan @TriggerLoop

Triangulation code is from https://github.com/gpvigano/AsImpL

My original inspiration was 3DS Max's Hedra plugin which kept me entertained for quite a while nearly 2 decades ago. I think credit for that is due to Tom Hudson :-)

![Screenshot](https://github.com/Ixxy-Open-Source/wythoff-polyhedra/blob/master/0.png)

