import socket

SERVER_IP = "127.0.0.1"  # Server host IP
SERVER_PORT = 50601  # Server port for receiving communication

class Server:

    def __init__(self, ip, port) -> None:
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)  # UDP socket creation
        self.sock.bind((ip, port))  # Needs to be tuple (string, int)

    def receive(self):
        data = None
        while data is None:
            data, self.client = self.sock.recvfrom(1024)  # Buffer size is 1024 bytes
            print("Received message: %s" % data)
        return str(data, encoding="utf-8")

    def send_response(self):
        self.sock.sendto(b"Message received...", self.client)

    def send_last_response(self):
        self.sock.sendto(b"End connection message received... closing connection", self.client)

    def quit(self):
        self.sock.close()  # Correctly closing socket
        print("Server closed..")


if __name__ == "__main__":
    server = Server(SERVER_IP, SERVER_PORT)
    data = "empty"

    while data != "End connection":
        if data != "empty":
            server.send_response()
        data = server.receive()

    server.send_last_response()
    server.quit()
