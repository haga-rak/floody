 # floody

**This repository c**ontains a combination of an HTTP client (floody) and an HTTP server (floodys) 
that can be used to test the performance of an HTTP proxy.

```
+---------+       +-----------+       +-----------+
|  floody |------>|   Proxy   |------>|  floodys  |
| (Client)|       | (to test) |       | (Server)  |
+---------+       +-----------+       +-----------+
```

## Build and run 


This project need .NET 9 SDK to build and run.

Clone the repository 
```bash
git clone https://github.com/haga-rak/floody.git
```
The build script (cmd and bash), created with Bulleyes.Target, can be used to bootstrap 
a test or a benchmark quickly. 

### Running a single session

Basic syntax:

```bash
./build.sh  test-https "floody-options: <floody-command-line-options>"
```
<sub>Use `build.cmd` on Windows</sub>

This command will :
- build the client (`floody`) in release mode 
- build the server (`floodys`) in release mode 
- start the web server on an available port 
- run the client against the server with the provided options
- **not start** the proxy server. The proxy server must be up and running before running the test.

Replace `test-https` with `test-http` to test in plain HTTP. 

For example, the following command tests the proxy at `127.0.0.1:44344` 
with 15 concurrent connections, during 10 seconds, and with a 5 seconds warm-up period.

```bash
./build.sh test-https "floody-options: -d 10 -w 5 -c 15 -x 127.0.0.1:44344"
```

### Running a benchmark session 

The command `./build.sh bench` can be used to generate a benchmark between multiple proxy server. 
By default, the no-proxy configuration is also tested. 

```bash
./build.sh bench "compare:44344 8080"
```

This command will :
- build the client (`floody`) in release mode
- build the server (`floodys`) in release mode
- start the web server on an available port
- Run a subsequent test session for 44344 8080.

The results will be saved in the directory `_results/`.

### floody 
`floody` is a simple wrapper around HttpClient that provides modest performance. By default, 
floody will not validate the server certificate.

#### Command line options:
```
Description:
  A simple http load test tool supporting proxy

Usage:
  floody <uri> [options]

Arguments:
  <uri>

Options:
  -X, --method <method>                                Method to used [default: GET]
  -c, --concurrent-connection <concurrent-connection>  Concurrent connection count to the remote [default: 8]
  -x, --proxy <proxy>                                  Address of HTTP proxy
  -r, --request-body-length <request-body-length>      Request body length
  -l, --response-body-length <response-body-length>    Response body length (sends `length` as query string, works 
                                                       only when used with floodys)
  -H, --header <header>                                Additional HTTP headers []
  -o, --output-file <output-file>                      Output benchmark result into a json file
  -d, --duration <duration>                            Test duration (unit accepted: ms, s, mn, h) [default: 00:00:30]
  -w, --warm-up <warm-up>                              Warm up duration (unit accepted: ms, s, mn, h) [default: 
                                                       00:00:05]
  --version                                            Show version information
  -?, -h, --help                                       Show help and usage information
```

### floodys 
`floodys` is a straightforward HTTP server based on Kestrel. When running in HTTPS mode, it uses a self-signed certificate `insecure.p12` available 
on this repository.  `floodys` takes the same arguments as a kestrel server with the addition of `--pid=` options which make the server
shutdown when the provided process id is terminated.






