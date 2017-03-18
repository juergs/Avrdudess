AVRDUDESS - A GUI for AVRDUDE
=============================

This is a GUI for AVRDUDE (http://savannah.nongnu.org/projects/avrdude).

http://blog.zakkemble.co.uk/avrdudess-a-gui-for-avrdude/

Windows:
--------
Requires .NET Framework 2.0 SP1 or newer (http://www.microsoft.com/en-gb/download/details.aspx?id=16614).
Latest .NET can be found here - http://www.microsoft.com/net

AVRDUDE requires LibUSB
LibUSB should really be installed the normal way as a driver for a LibUSB device, but if you don't have any such devices then you will need to download this - http://downloads.sourceforge.net/libusb-win32/libusb-win32-bin-1.2.6.0.zip

Extract libusb-win32-bin-1.2.6.0/bin/x86/libusb_x86.dll to where you have avrdude.exe placed and rename libusb_x86.dll to libusb.dll

Linux & Mac OS X:
-----------------
Can be ran using Mono (http://www.mono-project.com).

Has not been tested on OS X, but should work.

Installing on Ubuntu 13.10:
---------------------------
Install Mono (this is the minimum required, you can do mono-complete for a full install)

    sudo apt-get install libmono-winforms2.0-cil

Install AVRDUDE

    sudo apt-get install avrdude

Run AVRDUDESS with mono, you might have to run as root (sudo) so AVRDUDE runs as root if you havn't changed any rules.d stuff

    mono avrdudess.exe

--------

Zak Kemble

contact@zakkemble.co.uk


ATtiny841 added to avrdude.conf:

Thanks to:
https://www.ve7xen.com/blog/2014/03/07/programming-the-attiny841-with-avrdude/

http://www.avrfreaks.net/sites/default/files/forum_attachments/avrdude.conf__0.txt

Add this to avrrdude.conf:


#------------------------------------------------------------
# ATTiny841
#------------------------------------------------------------


part
     id            = "t841";
     desc          = "ATtiny841";
     has_debugwire = yes;
     flash_instr   = 0xB4, 0x07, 0x17;
     eeprom_instr  = 0xBB, 0xFF, 0xBB, 0xEE, 0xBB, 0xCC, 0xB2, 0x0D,
               0xBC, 0x07, 0xB4, 0x07, 0xBA, 0x0D, 0xBB, 0xBC,
               0x99, 0xE1, 0xBB, 0xAC;
## no STK500 devcode in XML file, use the ATtiny45 one
     stk500_devcode   = 0x14;
##  avr910_devcode   = ?;
##  Try the AT90S2313 devcode:
     avr910_devcode   = 0x20;
     signature        = 0x1e 0x93 0x15;
     reset            = io;
     chip_erase_delay = 4500;

     pgm_enable       = "1 0 1 0  1 1 0 0    0 1 0 1  0 0 1 1",
                        "x x x x  x x x x    x x x x  x x x x";

     chip_erase       = "1 0 1 0  1 1 0 0    1 0 0 x  x x x x",
                        "x x x x  x x x x    x x x x  x x x x";

    timeout       = 200;
    stabdelay     = 100;
    cmdexedelay       = 25;
    synchloops        = 32;
    bytedelay     = 0;
    pollindex     = 3;
    pollvalue     = 0x53;
    predelay      = 1;
    postdelay     = 1;
    pollmethod        = 1;

    hvsp_controlstack   =
        0x4C, 0x0C, 0x1C, 0x2C, 0x3C, 0x64, 0x74, 0x66,
        0x68, 0x78, 0x68, 0x68, 0x7A, 0x6A, 0x68, 0x78,
        0x78, 0x7D, 0x6D, 0x0C, 0x80, 0x40, 0x20, 0x10,
        0x11, 0x08, 0x04, 0x02, 0x03, 0x08, 0x04, 0x0F;
    hventerstabdelay    = 100;
    hvspcmdexedelay     = 0;
    synchcycles         = 6;
    latchcycles         = 1;
    togglevtg           = 1;
    poweroffdelay       = 25;
    resetdelayms        = 0;
    resetdelayus        = 70;
    hvleavestabdelay    = 100;
    resetdelay          = 25;
    chiperasepolltimeout = 40;

     memory "eeprom"
         size            = 512;
        paged           = no;
        page_size       = 4;
         min_write_delay = 4000;
         max_write_delay = 4500;
         readback_p1     = 0xff;
         readback_p2     = 0xff;
         read            = "1  0  1  0   0  0  0  0    0 0 0 x  x x x a8",
                           "a7 a6 a5 a4  a3 a2 a1 a0   o o o o  o o o o";

         write           = "1  1  0  0   0  0  0  0    0 0 0 x  x x x a8",
                           "a7 a6 a5 a4  a3 a2 a1 a0   i i i i  i i i i";

  loadpage_lo = "  1   1   0   0      0   0   0   1",
            "  0   0   0   0      0   0   0   0",
            "  0   0   0   0      0   0  a1  a0",
            "  i   i   i   i      i   i   i   i";

  writepage   = "  1   1   0   0      0   0   1   0",
            "  0   0   x   x      x   x   x   x",
            "  x  a6  a5  a4     a3  a2   0   0",
            "  x   x   x   x      x   x   x   x";

  mode        = 0x41;
  delay       = 6;
  blocksize   = 4;
  readsize    = 256;
       ;
     memory "flash"
         paged           = yes;
         size            = 8192;
         page_size       = 16;
         num_pages       = 512;
         min_write_delay = 4500;
         max_write_delay = 4500;
         readback_p1     = 0xff;
         readback_p2     = 0xff;
         read_lo         = "  0   0   1   0    0   0   0   0",
                           "  0   0   0   0  a11 a10  a9  a8",
                           " a7  a6  a5  a4   a3  a2  a1  a0",
                           "  o   o   o   o    o   o   o   o";

         read_hi         = "  0   0   1   0    1   0   0   0",
                           "  0   0   0   0  a11 a10  a9  a8",
                           " a7  a6  a5  a4   a3  a2  a1  a0",
                           "  o   o   o   o    o   o   o   o";

         loadpage_lo     = "  0   1   0   0    0   0   0   0",
                           "  0   0   0   x    x   x   x   x",
                           "  x   x   x   x    x  a2  a1  a0",
                           "  i   i   i   i    i   i   i   i";

         loadpage_hi     = "  0   1   0   0    1   0   0   0",
                           "  0   0   0   x    x   x   x   x",
                           "  x   x   x   x    x  a2  a1  a0",
                           "  i   i   i   i    i   i   i   i";

         writepage       = "  0  1  0  0   1   1   0  0",
                           "  0  0  0  0  a11 a10 a9 a8",
                           " a7 a6 a5 a4  a3  x  x  x",
                           "  x  x  x  x   x  x  x  x";

  mode        = 0x41;
  delay       = 10;
  blocksize   = 16;
  readsize    = 256;
       ;
#   ATtiny841 has Signature Bytes: 0x1E 0x93 0x0C.
     memory "signature"
         size            = 3;
         read            = "0  0  1  1   0  0  0  0   0  0  0  x   x  x  x  x",
                           "x  x  x  x   x  x a1 a0   o  o  o  o   o  o  o  o";
       ;

     memory "lock"
         size            = 1;
         write           = "1 0 1 0  1 1 0 0  1 1 1 x  x x x x",
                           "x x x x  x x x x  x x x x  x x i i";
         read            = "0 1 0 1  1 0 0 0  0 0 0 0  0 0 0 0",
                           "0 0 0 0  0 0 0 0  o o o o  o o o o";
        min_write_delay = 9000;
        max_write_delay = 9000;
       ;

     memory "lfuse"
         size            = 1;
         write           = "1 0 1 0  1 1 0 0  1 0 1 0  0 0 0 0",
                           "x x x x  x x x x  i i i i  i i i i";

         read            = "0 1 0 1  0 0 0 0  0 0 0 0  0 0 0 0",
                           "x x x x  x x x x  o o o o  o o o o";
        min_write_delay = 9000;
        max_write_delay = 9000;
       ;

     memory "hfuse"
         size            = 1;
         write           = "1 0 1 0  1 1 0 0  1 0 1 0  1 0 0 0",
                           "x x x x  x x x x  i i i i  i i i i";

         read            = "0 1 0 1  1 0 0 0  0 0 0 0  1 0 0 0",
                           "x x x x  x x x x  o o o o  o o o o";
        min_write_delay = 9000;
        max_write_delay = 9000;
       ;

     memory "efuse"
         size            = 1;
         write           = "1 0 1 0  1 1 0 0  1 0 1 0  0 1 0 0",
                           "x x x x  x x x x  x x x x  x x x i";

         read            = "0 1 0 1  0 0 0 0  0 0 0 0  1 0 0 0",
                           "x x x x  x x x x  o o o o  o o o o";
        min_write_delay = 9000;
        max_write_delay = 9000;
     ;

     memory "calibration"
         size            = 1;
         read            = "0  0  1  1   1  0  0  0    0 0 0 x  x x x x",
                           "0  0  0  0   0  0  0  a0   o o o o  o o o o";
     ;
  ;

