 # floody

This repository contains a simple performance testing tool for evaluating HTTP proxy servers. 
It includes an HTTP client (`floody`) to simulate traffic and an HTTP server 
(`floodys`) based on Kestrel to handle requests.

The current build supports configurable options; concurrent connections, 
request durations, request and response size, ...

You can also use it to benchmark multiple proxy setups or comparing performance with and without a proxy.

```
+---------+       +-----------+       +-----------+
|  floody |------>|   Proxy   |------>|  floodys  |
| (Client)|       | (to test) |       | (Server)  |
+---------+       +-----------+       +-----------+
```

## Build and run 

This project need :
- .NET 9 SDK to build and run.
- `clang` or `gcc` for Linux (by default NativeAOT is enabled). You can set environment variable `DISABLE_AOT` to `1` to dispense from using NativeAOT.

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
- **NOT START** the proxy server. The proxy server must be up and running before running the test.

Replace `test-https` with `test-http` to test in plain HTTP. 

For example, the following command tests the proxy at `127.0.0.1:44344` 
with 15 concurrent connections, during 10 seconds, and with a 5 seconds warm-up period.

```bash
./build.sh test-https "floody-options: -d 10 -w 5 -c 15 -x 127.0.0.1:44344"
```

Use `floody-target:<host-name>` to specify the host name of the server if it is different from `127.0.0.1`.

### Running a benchmark session 

Use the following command to run a benchmark across multiple proxy servers:

```bash
./build.sh bench "compare:44344 8080"
```
By default, the no-proxy configuration is also tested. Results will be saved in the `_results/` directory.

### floody 
`floody` is a simple wrapper around `HttpClient` that provides modest performance. By default, it does not validate server certificates.

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
This is an example of an execution result: 

```json
{
  "options": {
    "httpSettings": {
      "proxy": "http://127.0.0.1:44344/",
      "uri": "http://127.0.0.1:38811/",
      "method": "GET",
      "concurrentConnection": 16,
      "additionalHeaders": [],
      "requestBodyLength": 0,
      "responseBodyLength": 0
    },
    "startupSettings": {
      "duration": "00:00:15",
      "durationSeconds": 15,
      "warmupDuration": "00:00:05",
      "warmupDurationSeconds": 5
    }
  },
  "count": 1506506,
  "successCount": 1506506,
  "httpFailCount": 0,
  "networkFailCount": 0,
  "requestPerSeconds": 100433.73333333334,
  "totalSentBytes": 94910886,
  "totalReceivedBytes": 198858792,
  "totalSentPerSeconds": "6.03 MB/s",
  "totalReceivedPerSeconds": "12.64 MB/s"
}
```

### floodys 
`floodys` is a simple HTTP server based on Kestrel. When running in HTTPS mode, it uses the self-signed certificate `insecure.p12`, included in this repository.
`floodys` accepts the same arguments as a standard Kestrel server, with the addition of the `--pid=<pid>` option. This option ensures the server shuts down when the specified process ID terminates.

## LICENSE
This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.



