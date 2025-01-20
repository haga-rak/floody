 # floody

**This repository c**ontains a combination of an HTTP client (floody) and an HTTP server (floodys) 
that can be used to test the performance of an HTTP proxy.
Without the intermediary, this configuration can generate 300K requests per second.

```
+---------+       +-----------+       +-----------+
|  floody |------>|   Proxy   |------>|  floodys  |
| (Client)|       | (to test) |       | (Server)  |
+---------+       +-----------+       +-----------+
```

## Build and run 

You will need .NET 9.0 SDK, git and clang in order to build and run this project.

Clone the repository 
```bash
git clone https://github.com/haga-rak/floody.git
```

This project is using BullsEyes target to orchestrate the test. It builds release versions of
the client and server and runs the tests.

```bash
build benchmarkhttps "floody: -d 10 -c 15 -x proxy-address:proxy-port"
```





