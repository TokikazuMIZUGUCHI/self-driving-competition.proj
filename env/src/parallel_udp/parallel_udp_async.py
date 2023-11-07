import asyncio
import asyncio_dgram

class ParallelUDPServerAsync:
    def __init__(self, ip, src, info_dst, action_dst, generation_dst, lost_dst):
        self.ip = ip
        self.src = src
        self.info_dst = info_dst
        self.action_dst = action_dst
        self.generation_dst = generation_dst
        self.lost_dst = lost_dst

    async def read(self, on_receive):
        self.stream = await asyncio_dgram.bind((self.ip, self.src))
        while True:
            data, addr = await self.stream.recv()
            text = data.decode(encoding='utf8')
            action_text, generation_text, lost_text = on_receive(text)
            if (action_text != "") :
                await self.stream.send(action_text.encode('utf-8'), (self.ip, self.action_dst))
            if (generation_text != "") :
                await self.stream.send(generation_text.encode('utf-8'), (self.ip, self.generation_dst))
            if (lost_text is not None) :
                await self.stream.send(lost_text.encode('utf-8'), (self.ip, self.lost_dst))

    def close(self):
        self.stream.close()
