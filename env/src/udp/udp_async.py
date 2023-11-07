import asyncio
import asyncio_dgram

class UDPServerAsync:
    def __init__(self, ip, src, dst):
        self.ip = ip
        self.src = src
        self.dst = dst

    async def read(self, on_receive):
        self.stream = await asyncio_dgram.bind((self.ip, self.src))
        while True:
            data, addr = await self.stream.recv()
            text = data.decode(encoding='utf8')
            text = on_receive(text)
            await self.stream.send(text.encode('utf-8'), (self.ip, self.dst))

    def close(self):
        self.stream.close()
