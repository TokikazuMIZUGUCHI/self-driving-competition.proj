import socket

class UDPServer:
    def __init__(self, host, srcPort, dstPort, on_receive):
        self.host = host
        self.port = dstPort
        self.on_receive = on_receive

        self.server = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
        self.server.bind((host, srcPort))

    def send(self, text):
        self.server.sendto(text.encode('utf-8'), (self.host, self.port))

    def receive(self):
        message, addr = self.server.recvfrom(2048)
        if len(message) > 0:
            text = message.decode(encoding='utf8')
            text = self.on_receive(text)
            self.server.sendto(text.encode('utf-8'), (self.host, self.port))

    def close(self):
        self.server.close()
        self.is_receive = False
