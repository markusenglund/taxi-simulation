from manim import *

# config.frame_height = 10
# config.frame_width = 10

DEFAULT_FONT_SIZE = 26
Text.set_default(font="sans-serif", font_size=DEFAULT_FONT_SIZE)

class ValueOfTimeGraph(Scene):
    def construct(self):
      self.camera.background_color = "#444444"
      
      axes = Axes(
        x_range=[0, 80, 20],
        y_range=[0, 120, 20],
        x_length=8,
        y_length=5.5,
        axis_config={
           "include_numbers": True,
           "label_constructor": Text,
           "font_size": 20,
        },
      ).shift(UP*0.3)
      
      x_label = axes.get_x_axis_label(Text("Income ($/hr)")).shift(LEFT*5.3 + DOWN*1.3)
      y_label = axes.get_y_axis_label(Text("Value of time ($/hr)")).shift(LEFT*2.8 + DOWN*3.1).rotate(90 * DEGREES)
      surgeLabel = MathTex("y = 10 \sqrt{x}", color=YELLOW).scale(0.7).shift(1.5*UP + 0.8*RIGHT)
      
      surgeGraph = axes.plot(lambda x: 10 * np.sqrt(x), color=YELLOW, x_range=[4, 80])
      self.add(axes, x_label, y_label)
      self.play(Create(surgeGraph))
      self.play(FadeIn(surgeLabel))
      self.wait(4)
