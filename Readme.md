---

# MIPS Multicore Processor Simulation 
## Computer Architecture
__University of Costa Rica__

---


Simulation of a MIPS multiprocessor machine running parallelized instructions. The parallelization is on a instruction level (i.e. the pipeline level is not simulated).

* Language: C#

### Compiling and running

**Linux**
You will need [Mono](http://www.mono-project.com/download/) in order to compile and run C# code on linux. To install mono on linux, run the following command on a terminal:

```bash
    sudo apt-get install mono-complete # to install mono on linux
```

Once finished, run the following commands on a terminal to compile and run the code:

```
    mcs *.cs # to compile
    mono Computer.exe # to run
```

**Windows and Mac**
To run the simulation on Windows or Mac, you can either use Mono, compile and run as described above for linux, or use **Visual Studio**:
* [for Mac](https://www.visualstudio.com/es/vs/visual-studio-mac/)
* [for windows](https://www.visualstudio.com/es/)

To run on Visual Studio, open the `.sln` solution file that is inside this project folder. Once the project is loaded on Visual Studio, just press F5 key to compile and run it.